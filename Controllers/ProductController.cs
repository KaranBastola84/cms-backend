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
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public ProductController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        /// <summary>
        /// Get all products with optional filters and pagination (Public)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllProducts(
            [FromQuery] string? category = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool? isFeatured = null,
            [FromQuery] bool? lowStock = null,
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

                var query = _context.Products.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(p => p.Category.ToLower() == category.ToLower());
                }

                if (isActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == isActive.Value);
                }

                if (isFeatured.HasValue)
                {
                    query = query.Where(p => p.IsFeatured == isFeatured.Value);
                }

                if (lowStock.HasValue && lowStock.Value)
                {
                    query = query.Where(p => p.StockQuantity <= p.LowStockThreshold);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()) ||
                                            p.Description.ToLower().Contains(search.ToLower()));
                }

                var totalCount = await query.CountAsync();

                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductListDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        Category = p.Category,
                        ImageUrl = p.ImageUrl,
                        IsActive = p.IsActive,
                        IsFeatured = p.IsFeatured,
                        IsLowStock = p.StockQuantity <= p.LowStockThreshold
                    })
                    .ToListAsync();

                return Ok(ResponseHelper.Success(new
                {
                    products,
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
                return StatusCode(500, ResponseHelper.Error<object>($"Error retrieving products: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Get product by ID (Public)
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var product = await _context.Products
                    .Where(p => p.Id == id)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        LowStockThreshold = p.LowStockThreshold,
                        Category = p.Category,
                        ImageUrl = p.ImageUrl,
                        IsActive = p.IsActive,
                        IsFeatured = p.IsFeatured,
                        IsLowStock = p.StockQuantity <= p.LowStockThreshold,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Product not found", 404));
                }

                return Ok(ResponseHelper.Success(product));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error retrieving product: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Get featured products (Public)
        /// </summary>
        [HttpGet("featured")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFeaturedProducts([FromQuery] int limit = 10)
        {
            try
            {
                if (limit < 1) limit = 10;
                if (limit > 50) limit = 50;

                var products = await _context.Products
                    .Where(p => p.IsFeatured && p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(limit)
                    .Select(p => new ProductListDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        Category = p.Category,
                        ImageUrl = p.ImageUrl,
                        IsActive = p.IsActive,
                        IsFeatured = p.IsFeatured,
                        IsLowStock = p.StockQuantity <= p.LowStockThreshold
                    })
                    .ToListAsync();

                return Ok(ResponseHelper.Success(products));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error retrieving featured products: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Get products by category (Public)
        /// </summary>
        [HttpGet("category/{category}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductsByCategory(string category, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Products
                    .Where(p => p.Category.ToLower() == category.ToLower() && p.IsActive);

                var totalCount = await query.CountAsync();

                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductListDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        Category = p.Category,
                        ImageUrl = p.ImageUrl,
                        IsActive = p.IsActive,
                        IsFeatured = p.IsFeatured,
                        IsLowStock = p.StockQuantity <= p.LowStockThreshold
                    })
                    .ToListAsync();

                return Ok(ResponseHelper.Success(new
                {
                    category,
                    products,
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
                return StatusCode(500, ResponseHelper.Error<object>($"Error retrieving products by category: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Create a new product (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ResponseHelper.Error<object>("Invalid product data"));
                }

                var product = new Product
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Price = dto.Price,
                    StockQuantity = dto.StockQuantity,
                    LowStockThreshold = dto.LowStockThreshold,
                    Category = dto.Category,
                    IsActive = dto.IsActive,
                    IsFeatured = dto.IsFeatured,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Log product creation
                await _auditService.LogAsync(
                    ActionType.CREATE,
                    "Product",
                    product.Id.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(new { product.Name, product.Price, product.StockQuantity }),
                    $"Product created: {product.Name}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );

                return Ok(ResponseHelper.Success(new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    LowStockThreshold = product.LowStockThreshold,
                    Category = product.Category,
                    ImageUrl = product.ImageUrl,
                    IsActive = product.IsActive,
                    IsFeatured = product.IsFeatured,
                    IsLowStock = product.StockQuantity <= product.LowStockThreshold,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt
                }, "Product created successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error creating product: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Update a product (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ResponseHelper.Error<object>("Invalid product data"));
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Product not found", 404));
                }

                // Store old values for audit
                var oldValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    product.Name,
                    product.Price,
                    product.StockQuantity,
                    product.IsActive,
                    product.IsFeatured
                });

                // Update product
                product.Name = dto.Name;
                product.Description = dto.Description;
                product.Price = dto.Price;
                product.StockQuantity = dto.StockQuantity;
                product.LowStockThreshold = dto.LowStockThreshold;
                product.Category = dto.Category;
                product.IsActive = dto.IsActive;
                product.IsFeatured = dto.IsFeatured;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log product update
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Product",
                    product.Id.ToString(),
                    oldValues,
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        product.Name,
                        product.Price,
                        product.StockQuantity,
                        product.IsActive,
                        product.IsFeatured
                    }),
                    $"Product updated: {product.Name}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );

                return Ok(ResponseHelper.Success(new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    LowStockThreshold = product.LowStockThreshold,
                    Category = product.Category,
                    ImageUrl = product.ImageUrl,
                    IsActive = product.IsActive,
                    IsFeatured = product.IsFeatured,
                    IsLowStock = product.StockQuantity <= product.LowStockThreshold,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt
                }, "Product updated successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error updating product: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Update product stock quantity (Admin only)
        /// </summary>
        [HttpPut("{id}/stock")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ResponseHelper.Error<object>("Invalid stock data"));
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Product not found", 404));
                }

                var oldStock = product.StockQuantity;
                product.StockQuantity = dto.StockQuantity;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log stock update
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Product",
                    product.Id.ToString(),
                    System.Text.Json.JsonSerializer.Serialize(new { StockQuantity = oldStock }),
                    System.Text.Json.JsonSerializer.Serialize(new { StockQuantity = product.StockQuantity }),
                    $"Stock updated for {product.Name}: {oldStock} → {product.StockQuantity}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );

                return Ok(ResponseHelper.Success(new
                {
                    productId = product.Id,
                    productName = product.Name,
                    oldStock,
                    newStock = product.StockQuantity,
                    isLowStock = product.StockQuantity <= product.LowStockThreshold
                }, "Stock updated successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error updating stock: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Delete a product (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Product not found", 404));
                }

                // Check if product is in any orders
                var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
                if (hasOrders)
                {
                    return BadRequest(ResponseHelper.Error<object>("Cannot delete product that has orders. Consider deactivating it instead."));
                }

                // Log deletion before removing
                await _auditService.LogAsync(
                    ActionType.DELETE,
                    "Product",
                    product.Id.ToString(),
                    System.Text.Json.JsonSerializer.Serialize(new { product.Name, product.Price, product.StockQuantity }),
                    null,
                    $"Product deleted: {product.Name}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(ResponseHelper.Success(new { message = "Product deleted successfully" }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error deleting product: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Get available product categories (Public)
        /// </summary>
        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Products
                    .Where(p => p.IsActive)
                    .Select(p => p.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(ResponseHelper.Success(categories));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error retrieving categories: {ex.Message}", 500));
            }
        }
    }
}
