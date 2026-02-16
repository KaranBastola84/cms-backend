using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class PayInstallmentDto
    {
        [Required]
        public int InstallmentId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Stripe"; // "Stripe", "Cash", "eSewa"

        [StringLength(500)]
        public string? Remarks { get; set; }
    }
}
