using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JWTAuthAPI.Models
{
    public class StripePayment
    {
        [Key]
        public int StripePaymentId { get; set; }

        [Required]
        [StringLength(255)]
        public string PaymentIntentId { get; set; } = string.Empty;

        [Required]
        public int StudentId { get; set; }

        public int? InstallmentId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "usd";

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [StringLength(100)]
        public string? PaymentMethod { get; set; }

        [StringLength(255)]
        public string? ClientSecret { get; set; }

        [StringLength(1000)]
        public string? Metadata { get; set; }

        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("InstallmentId")]
        public virtual Installment? Installment { get; set; }
    }
}
