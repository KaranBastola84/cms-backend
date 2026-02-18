using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    // DTO for creating a product review
    public class CreateProductReviewDto
    {
        [Required(ErrorMessage = "Product ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid product ID")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Customer name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Review text cannot exceed 1000 characters")]
        public string? ReviewText { get; set; }
    }

    // DTO for product review response
    public class ProductReviewDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? ReviewText { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // DTO for approving a review
    public class ApproveReviewDto
    {
        [Required(ErrorMessage = "Approval status is required")]
        public bool IsApproved { get; set; }
    }
}
