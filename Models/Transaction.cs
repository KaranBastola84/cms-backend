using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JWTAuthAPI.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } = string.Empty; // "Payment", "Receipt", "Refund"

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [StringLength(50)]
        public string? PaymentGateway { get; set; } // "Stripe", "eSewa", "Cash", etc.

        [Required]
        public PaymentStatus Status { get; set; }

        [StringLength(255)]
        public string? ReferenceNumber { get; set; } // Payment Intent ID or Receipt Number

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string ProcessedBy { get; set; } = string.Empty;

        // Navigation property
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }
    }
}
