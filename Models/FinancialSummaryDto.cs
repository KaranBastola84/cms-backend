namespace JWTAuthAPI.Models
{
    public class FinancialSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal OutstandingAmount { get; set; }
        public decimal TotalExpectedRevenue { get; set; }
        public int TotalStudents { get; set; }
        public int StudentsWithPaymentPlans { get; set; }
        public int TotalPaymentPlans { get; set; }
        public int ActivePaymentPlans { get; set; }
        public int CompletedPaymentPlans { get; set; }
        public int DefaultedPaymentPlans { get; set; }
        public int SuspendedPaymentPlans { get; set; }
        public int OverdueInstallments { get; set; }
        public decimal OverdueAmount { get; set; }
        public int TotalPaidInstallments { get; set; }
        public int TotalPendingInstallments { get; set; }
        public int TotalOverdueInstallments { get; set; }
        public decimal CollectionRate { get; set; }
        public decimal TodayCollection { get; set; }
        public decimal MonthCollection { get; set; }
        public decimal YearCollection { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
