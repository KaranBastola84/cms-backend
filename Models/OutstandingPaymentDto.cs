namespace JWTAuthAPI.Models
{
    public class OutstandingPaymentDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentPhone { get; set; } = string.Empty;
        public int PaymentPlanId { get; set; }
        public int? CourseId { get; set; }
        public string? CourseName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int PendingInstallments { get; set; }
        public int OverdueInstallments { get; set; }
        public int OverdueInstallmentsCount { get; set; }
        public decimal OverdueAmount { get; set; }
        public DateTime? NextDueDate { get; set; }
        public int DaysOverdue { get; set; }
    }
}
