namespace JWTAuthAPI.Models
{
    public class CourseRevenueDto
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int TotalEnrolled { get; set; }
        public decimal TotalExpectedRevenue { get; set; }
        public decimal TotalCollectedRevenue { get; set; }
        public decimal TotalOutstanding { get; set; }
        public int ActivePlans { get; set; }
        public int CompletedPlans { get; set; }
        public int DefaultedPlans { get; set; }
        public decimal CollectionRate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
