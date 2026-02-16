using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class CreateStripePaymentDto
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public int InstallmentId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [StringLength(3)]
        public string Currency { get; set; } = "usd";

        public Dictionary<string, string>? Metadata { get; set; }
    }
}
