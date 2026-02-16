using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JWTAuthAPI.Models
{
    public class Installment
    {
        [Key]
        public int InstallmentId { get; set; }

        [Required]
        public int PaymentPlanId { get; set; }

        [Required]
        public int InstallmentNumber { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public DateTime? PaidDate { get; set; }

        [Required]
        public InstallmentStatus Status { get; set; } = InstallmentStatus.Pending;

        public int? ReceiptId { get; set; }

        [StringLength(255)]
        public string? StripePaymentIntentId { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PaymentPlanId")]
        public virtual PaymentPlan? PaymentPlan { get; set; }

        [ForeignKey("ReceiptId")]
        public virtual Receipt? Receipt { get; set; }
    }
}
