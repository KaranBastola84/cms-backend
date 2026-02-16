namespace JWTAuthAPI.Models
{
    public class RevenueReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalPayments { get; set; }
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
