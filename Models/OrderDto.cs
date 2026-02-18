using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    // DTO for creating a new order
    public class CreateOrderDto
    {
        [Required(ErrorMessage = "Customer name is required")]
        [StringLength(100, ErrorMessage = "Customer name cannot exceed 100 characters")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Delivery address is required")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string DeliveryAddress { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? CustomerNotes { get; set; }

        [Required(ErrorMessage = "Order items are required")]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<CreateOrderItemDto> OrderItems { get; set; } = new();
    }

    // DTO for order items in create order
    public class CreateOrderItemDto
    {
        [Required(ErrorMessage = "Product ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid product ID")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }

    // DTO for order details response
    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string? CustomerNotes { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? AdminNotes { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public List<OrderItemDto> OrderItems { get; set; } = new();
    }

    // DTO for order items in response
    public class OrderItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
    }

    // DTO for order list (lightweight)
    public class OrderListDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public int ItemCount { get; set; }
    }

    // DTO for updating order status
    public class UpdateOrderStatusDto
    {
        [Required(ErrorMessage = "Status is required")]
        public OrderStatus Status { get; set; }

        [StringLength(1000, ErrorMessage = "Admin notes cannot exceed 1000 characters")]
        public string? AdminNotes { get; set; }
    }

    // DTO for updating payment status
    public class UpdatePaymentStatusDto
    {
        [Required(ErrorMessage = "Payment status is required")]
        public PaymentStatus PaymentStatus { get; set; }

        [StringLength(1000, ErrorMessage = "Admin notes cannot exceed 1000 characters")]
        public string? AdminNotes { get; set; }
    }
}
