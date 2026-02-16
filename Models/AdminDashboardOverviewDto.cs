namespace JWTAuthAPI.Models
{
    public class AdminDashboardOverviewDto
    {
        public StudentStatistics Students { get; set; } = new();
        public CourseStatistics Courses { get; set; } = new();
        public BatchStatistics Batches { get; set; } = new();
        public StaffStatistics Staff { get; set; } = new();
        public InquiryStatistics Inquiries { get; set; } = new();
    }

    public class StudentStatistics
    {
        public int Total { get; set; }
        public int PendingPayment { get; set; }
        public int Enrolled { get; set; }
        public int Active { get; set; }
        public int Completed { get; set; }
        public int Dropped { get; set; }
        public int Suspended { get; set; }
        public int NewThisWeek { get; set; }
        public int NewThisMonth { get; set; }
        public int NewToday { get; set; }
    }

    public class CourseStatistics
    {
        public int Total { get; set; }
        public int Active { get; set; }
        public int Inactive { get; set; }
    }

    public class BatchStatistics
    {
        public int Total { get; set; }
        public int Active { get; set; }
        public int Upcoming { get; set; }
        public int Completed { get; set; }
        public decimal AverageCapacityUtilization { get; set; }
    }

    public class StaffStatistics
    {
        public int TotalStaff { get; set; }
        public int TotalTrainers { get; set; }
        public int TotalUsers { get; set; }
    }

    public class InquiryStatistics
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int FollowedUp { get; set; }
        public int Closed { get; set; }
    }
}
