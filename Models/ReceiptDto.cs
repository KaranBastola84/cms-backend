using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class CreateReceiptDto
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public ReceiptType ReceiptType { get; set; }

        public string? Description { get; set; }

        public DateTime? PaymentDate { get; set; }

        public string? PaymentMethod { get; set; }
    }

    public class ReceiptDto
    {
        public int ReceiptId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string ReceiptType { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
    }
}
