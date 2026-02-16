namespace JWTAuthAPI.Models
{
    public class AdminFinancialSummaryDto
    {
        public RevenueMetrics Revenue { get; set; } = new();
        public OutstandingMetrics Outstanding { get; set; } = new();
        public CollectionMetrics Collection { get; set; } = new();
        public PaymentPlanMetrics PaymentPlans { get; set; } = new();
        public UpcomingPayments UpcomingPayments { get; set; } = new();
    }

    public class RevenueMetrics
    {
        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueThisWeek { get; set; }
        public decimal RevenueToday { get; set; }
        public decimal AverageRevenuePerStudent { get; set; }
    }

    public class OutstandingMetrics
    {
        public decimal TotalOutstanding { get; set; }
        public decimal OverdueAmount { get; set; }
        public int OverdueInstallments { get; set; }
        public int DefaultersCount { get; set; }
        public int StudentsPendingFirstPayment { get; set; }
    }

    public class CollectionMetrics
    {
        public decimal CollectionRate { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public decimal CollectedRevenue { get; set; }
    }

    public class PaymentPlanMetrics
    {
        public int ActivePlans { get; set; }
        public int CompletedPlans { get; set; }
        public int DefaultedPlans { get; set; }
        public int SuspendedPlans { get; set; }
        public int CancelledPlans { get; set; }
    }

    public class UpcomingPayments
    {
        public int DueNext7Days { get; set; }
        public decimal AmountDueNext7Days { get; set; }
        public int DueNext30Days { get; set; }
        public decimal AmountDueNext30Days { get; set; }
    }
}
