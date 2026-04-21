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
        /// <summary>
        /// Tuition + Stripe Revenue (includes all tuition payments via Stripe, both installment and direct). Display as: "Tuition + Stripe Revenue"
        /// </summary>
        public decimal TotalStripeRevenue { get; set; }
        /// <summary>
        /// Cash/QR/Order Revenue (includes all cash payments and paid shop orders). Display as: "Cash/QR/Order Revenue"
        /// </summary>
        public decimal TotalCashRevenue { get; set; }
        /// <summary>
        /// Number of cash/QR payments and paid orders. Display as: "Cash/QR/Order Payments"
        /// </summary>
        public int TotalCashPayments { get; set; }
        public decimal YearCollection { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
