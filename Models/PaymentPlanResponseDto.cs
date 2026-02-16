namespace JWTAuthAPI.Models
{
    public class PaymentPlanResponseDto
    {
        public int PaymentPlanId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int? CourseId { get; set; }
        public string? CourseName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public int NumberOfInstallments { get; set; }
        public PaymentPlanStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public List<InstallmentResponseDto> Installments { get; set; } = new List<InstallmentResponseDto>();
    }
}
