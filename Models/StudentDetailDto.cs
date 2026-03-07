namespace JWTAuthAPI.Models
{
    public class StudentDetailDto
    {
        // Student info
        public int StudentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int? CourseId { get; set; }
        public int? BatchId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? EmergencyContact { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? AdmissionDate { get; set; }

        // Fee summary
        public decimal FeesPaid { get; set; }
        public decimal FeesTotal { get; set; }
        public decimal FeesRemaining { get; set; }
        public string? ReceiptNumber { get; set; }

        // Stripe payment summary
        public int TotalPayments { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public List<StripePaymentResponseDto> RecentPayments { get; set; } = new();

        // Document summary
        public int TotalDocuments { get; set; }
        public List<StudentDocumentDto> Documents { get; set; } = new();
    }
}
