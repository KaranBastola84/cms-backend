using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class CashPayment
    {
        public int CashPaymentId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        [Required]
        [StringLength(100)]
        public string ProcessedBy { get; set; } = string.Empty;

        public DateTime PaidAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Student? Student { get; set; }
    }

    public class CashPaymentRecordDto
    {
        public int CashPaymentId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Remarks { get; set; }
        public string ProcessedBy { get; set; } = string.Empty;
        public DateTime PaidAt { get; set; }
    }
}
