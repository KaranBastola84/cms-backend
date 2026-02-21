using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JWTAuthAPI.Data;
using JWTAuthAPI.Models;
using JWTAuthAPI.Helpers;
using JWTAuthAPI.Services;

namespace JWTAuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IEmailService _emailService;

        public OrderController(
            ApplicationDbContext context,
            IAuditService auditService,
            IEmailService emailService)
        {
            _context = context;
            _auditService = auditService;
            _emailService = emailService;
        }

        /// <summary>
        /// Place a new order (Public - no authentication required)
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> PlaceOrder([FromBody] CreateOrderDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ResponseHelper.Error<object>("Invalid order data"));
                }

                // Validate products and check stock availability
                var productIds = dto.OrderItems.Select(item => item.ProductId).ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id) && p.IsActive)
                    .ToListAsync();

                if (products.Count != productIds.Distinct().Count())
                {
                    return BadRequest(ResponseHelper.Error<object>("One or more products are not available"));
                }

                // Validate stock availability
                foreach (var item in dto.OrderItems)
                {
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product == null)
                    {
                        return BadRequest(ResponseHelper.Error<object>($"Product with ID {item.ProductId} not found"));
                    }

                    if (product.StockQuantity < item.Quantity)
                    {
                        return BadRequest(ResponseHelper.Error<object>($"Insufficient stock for {product.Name}. Available: {product.StockQuantity}"));
                    }
                }

                // Generate unique order number
                var orderNumber = await GenerateOrderNumberAsync();

                // Calculate total
                decimal totalAmount = 0;
                var orderItems = new List<OrderItem>();

                foreach (var item in dto.OrderItems)
                {
                    var product = products.First(p => p.Id == item.ProductId);
                    var subtotal = product.Price * item.Quantity;
                    totalAmount += subtotal;

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        UnitPrice = product.Price,
                        Quantity = item.Quantity,
                        Subtotal = subtotal
                    });
                }

                // Create order
                var order = new Order
                {
                    OrderNumber = orderNumber,
                    CustomerName = dto.CustomerName,
                    CustomerEmail = dto.CustomerEmail,
                    CustomerPhone = dto.CustomerPhone,
                    DeliveryAddress = dto.DeliveryAddress,
                    CustomerNotes = dto.CustomerNotes,
                    TotalAmount = totalAmount,
                    Status = OrderStatus.Pending,
                    PaymentStatus = PaymentStatus.Pending,
                    OrderDate = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    OrderItems = orderItems
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Log order creation
                await _auditService.LogAsync(
                    ActionType.CREATE,
                    "Order",
                    order.Id.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        order.OrderNumber,
                        order.CustomerEmail,
                        order.TotalAmount,
                        ItemCount = orderItems.Count
                    }),
                    $"New order placed: {order.OrderNumber}",
                    null,
                    order.CustomerEmail
                );

                await transaction.CommitAsync();

                // Send order confirmation email (fire and forget)
                _ = _emailService.SendOrderConfirmationEmailAsync(
                    order.CustomerEmail,
                    order.CustomerName,
                    order.OrderNumber,
                    order.TotalAmount
                );

                return Ok(ResponseHelper.Success(new
                {
                    orderId = order.Id,
                    orderNumber = order.OrderNumber,
                    totalAmount = order.TotalAmount,
                    status = order.Status.ToString(),
                    message = "Order placed successfully! You will be contacted soon."
                }));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ResponseHelper.Error<object>($"Error placing order: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Get all orders with filtering and pagination (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetAllOrders(
            [FromQuery] OrderStatus? status = null,
            [FromQuery] PaymentStatus? paymentStatus = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Validate pagination
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Orders.AsQueryable();

                // Apply filters
                if (status.HasValue)
                {
                    query = query.Where(o => o.Status == status.Value);
                }

                if (paymentStatus.HasValue)
                {
                    query = query.Where(o => o.PaymentStatus == paymentStatus.Value);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(o =>
                        o.OrderNumber.Contains(search) ||
                        o.CustomerName.ToLower().Contains(search.ToLower()) ||
                        o.CustomerEmail.ToLower().Contains(search.ToLower()) ||
                        o.CustomerPhone.Contains(search));
                }

                var totalCount = await query.CountAsync();

                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(o => o.OrderItems)
                    .Select(o => new OrderListDto
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        CustomerName = o.CustomerName,
                        CustomerEmail = o.CustomerEmail,
                        CustomerPhone = o.CustomerPhone,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status.ToString(),
                        PaymentStatus = o.PaymentStatus.ToString(),
                        OrderDate = o.OrderDate,
                        ItemCount = o.OrderItems.Count
                    })
                    .ToListAsync();

                return Ok(ResponseHelper.Success(new
                {
                    orders,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error retrieving orders: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Get order by ID (Admin only)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetOrderById(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.Id == id)
                    .Select(o => new OrderDto
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        CustomerName = o.CustomerName,
                        CustomerEmail = o.CustomerEmail,
                        CustomerPhone = o.CustomerPhone,
                        DeliveryAddress = o.DeliveryAddress,
                        CustomerNotes = o.CustomerNotes,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status.ToString(),
                        PaymentStatus = o.PaymentStatus.ToString(),
                        AdminNotes = o.AdminNotes,
                        OrderDate = o.OrderDate,
                        DeliveredDate = o.DeliveredDate,
                        PaidDate = o.PaidDate,
                        OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                        {
                            Id = oi.Id,
                            ProductId = oi.ProductId,
                            ProductName = oi.ProductName,
                            UnitPrice = oi.UnitPrice,
                            Quantity = oi.Quantity,
                            Subtotal = oi.Subtotal
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Order not found", 404));
                }

                return Ok(ResponseHelper.Success(order));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error retrieving order: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Update order status (Admin only) - Stock reduces when status changes to Confirmed
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ResponseHelper.Error<object>("Invalid status data"));
                }

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Order not found", 404));
                }

                var oldStatus = order.Status;

                // If changing to Confirmed, reduce stock
                if (dto.Status == OrderStatus.Confirmed && oldStatus != OrderStatus.Confirmed)
                {
                    foreach (var item in order.OrderItems)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            if (product.StockQuantity < item.Quantity)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest(ResponseHelper.Error<object>($"Insufficient stock for {product.Name}. Available: {product.StockQuantity}"));
                            }

                            product.StockQuantity -= item.Quantity;
                            product.UpdatedAt = DateTime.UtcNow;

                            // Check for low stock and log
                            if (product.StockQuantity <= product.LowStockThreshold)
                            {
                                await _auditService.LogAsync(
                                    ActionType.UPDATE,
                                    "Product",
                                    product.Id.ToString(),
                                    null,
                                    System.Text.Json.JsonSerializer.Serialize(new { product.StockQuantity, product.LowStockThreshold }),
                                    $"Low stock alert: {product.Name} - Stock: {product.StockQuantity}",
                                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                                );
                            }
                        }
                    }
                }

                // If changing back from Confirmed to another status, restore stock
                if (oldStatus == OrderStatus.Confirmed && dto.Status != OrderStatus.Confirmed)
                {
                    foreach (var item in order.OrderItems)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity += item.Quantity;
                            product.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                // Update order status
                order.Status = dto.Status;
                order.AdminNotes = dto.AdminNotes;
                order.UpdatedAt = DateTime.UtcNow;

                if (dto.Status == OrderStatus.Delivered && !order.DeliveredDate.HasValue)
                {
                    order.DeliveredDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Log status update
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Order",
                    order.Id.ToString(),
                    System.Text.Json.JsonSerializer.Serialize(new { Status = oldStatus.ToString() }),
                    System.Text.Json.JsonSerializer.Serialize(new { Status = order.Status.ToString() }),
                    $"Order {order.OrderNumber} status updated: {oldStatus} → {order.Status}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );

                await transaction.CommitAsync();

                return Ok(ResponseHelper.Success(new
                {
                    orderId = order.Id,
                    orderNumber = order.OrderNumber,
                    oldStatus = oldStatus.ToString(),
                    newStatus = order.Status.ToString(),
                    message = "Order status updated successfully"
                }));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ResponseHelper.Error<object>($"Error updating order status: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Update payment status (Admin only)
        /// </summary>
        [HttpPut("{id}/payment-status")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatusDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ResponseHelper.Error<object>("Invalid payment status data"));
                }

                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Order not found", 404));
                }

                var oldPaymentStatus = order.PaymentStatus;
                order.PaymentStatus = dto.PaymentStatus;

                if (!string.IsNullOrWhiteSpace(dto.AdminNotes))
                {
                    order.AdminNotes = dto.AdminNotes;
                }

                if (dto.PaymentStatus == PaymentStatus.Paid && !order.PaidDate.HasValue)
                {
                    order.PaidDate = DateTime.UtcNow;
                }

                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log payment status update
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Order",
                    order.Id.ToString(),
                    System.Text.Json.JsonSerializer.Serialize(new { PaymentStatus = oldPaymentStatus.ToString() }),
                    System.Text.Json.JsonSerializer.Serialize(new { PaymentStatus = order.PaymentStatus.ToString() }),
                    $"Order {order.OrderNumber} payment status updated: {oldPaymentStatus} → {order.PaymentStatus}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );

                return Ok(ResponseHelper.Success(new
                {
                    orderId = order.Id,
                    orderNumber = order.OrderNumber,
                    oldPaymentStatus = oldPaymentStatus.ToString(),
                    newPaymentStatus = order.PaymentStatus.ToString(),
                    paidDate = order.PaidDate,
                    message = "Payment status updated successfully"
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error updating payment status: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Get pending orders (Admin only)
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetPendingOrders()
        {
            try
            {
                var orders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Pending)
                    .OrderBy(o => o.OrderDate)
                    .Include(o => o.OrderItems)
                    .Select(o => new OrderListDto
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        CustomerName = o.CustomerName,
                        CustomerEmail = o.CustomerEmail,
                        CustomerPhone = o.CustomerPhone,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status.ToString(),
                        PaymentStatus = o.PaymentStatus.ToString(),
                        OrderDate = o.OrderDate,
                        ItemCount = o.OrderItems.Count
                    })
                    .ToListAsync();

                return Ok(ResponseHelper.Success(orders));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error retrieving pending orders: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Get orders by customer email (Admin only)
        /// </summary>
        [HttpGet("customer/{email}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetOrdersByCustomerEmail(string email)
        {
            try
            {
                var orders = await _context.Orders
                    .Where(o => o.CustomerEmail.ToLower() == email.ToLower())
                    .OrderByDescending(o => o.OrderDate)
                    .Include(o => o.OrderItems)
                    .Select(o => new OrderListDto
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        CustomerName = o.CustomerName,
                        CustomerEmail = o.CustomerEmail,
                        CustomerPhone = o.CustomerPhone,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status.ToString(),
                        PaymentStatus = o.PaymentStatus.ToString(),
                        OrderDate = o.OrderDate,
                        ItemCount = o.OrderItems.Count
                    })
                    .ToListAsync();

                return Ok(ResponseHelper.Success(orders));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error retrieving customer orders: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Generate unique order number
        /// </summary>
        private async Task<string> GenerateOrderNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var lastOrder = await _context.Orders
                .Where(o => o.OrderNumber.StartsWith($"ORD-{year}-"))
                .OrderByDescending(o => o.OrderNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastOrder != null)
            {
                var lastNumberStr = lastOrder.OrderNumber.Split('-').LastOrDefault();
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"ORD-{year}-{nextNumber:D4}";
        }
    }
}
