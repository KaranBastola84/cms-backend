namespace JWTAuthAPI.Models
{
    public class PaymentReportDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public int TotalInstallments { get; set; }
        public int PaidInstallments { get; set; }
        public int PendingInstallments { get; set; }
        public int OverdueInstallments { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? NextDueDate { get; set; }
        public PaymentPlanStatus Status { get; set; }
    }
}
