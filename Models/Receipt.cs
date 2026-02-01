using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JWTAuthAPI.Models
{
    public class Receipt
    {
        [Key]
        public int ReceiptId { get; set; }

        [Required]
        [StringLength(50)]
        public string ReceiptNumber { get; set; } = string.Empty;

        [Required]
        public int StudentId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public ReceiptType ReceiptType { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string GeneratedBy { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTime? PaymentDate { get; set; }

        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        // Navigation property
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }
    }

    public enum ReceiptType
    {
        AdmissionFee,
        TuitionFee,
        CourseFee,
        ExamFee,
        Other
    }
}
