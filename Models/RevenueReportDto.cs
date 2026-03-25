namespace JWTAuthAPI.Models
{
    public class RevenueReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        /// <summary>
        /// Tuition + Stripe Revenue (all tuition payments via Stripe, both installment and direct). Display as: "Tuition + Stripe Revenue"
        /// </summary>
        public decimal StripeRevenue { get; set; }
        /// <summary>
        /// Cash/QR/Order Revenue (all cash payments and paid shop orders). Display as: "Cash/QR/Order Revenue"
        /// </summary>
        public decimal CashRevenue { get; set; }
        public int TotalPayments { get; set; }
        public int CashPaymentCount { get; set; }
        public int UniquePayingStudents { get; set; }
        public List<CourseRevenueBreakdownDto> CourseRevenues { get; set; } = new();
        public decimal AveragePaymentAmount { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class CourseRevenueBreakdownDto
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int PaymentCount { get; set; }
        public int StudentCount { get; set; }
    }
}
