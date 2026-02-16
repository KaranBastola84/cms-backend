namespace JWTAuthAPI.Models
{
    public class StudentPaymentSummaryDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public List<PaymentPlanResponseDto> PaymentPlans { get; set; } = new List<PaymentPlanResponseDto>();
        public decimal TotalFeesPayable { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public int TotalInstallmentsDue { get; set; }
        public int TotalInstallmentsPaid { get; set; }
        public List<InstallmentResponseDto> UpcomingInstallments { get; set; } = new List<InstallmentResponseDto>();
        public List<InstallmentResponseDto> OverdueInstallments { get; set; } = new List<InstallmentResponseDto>();
    }
}
