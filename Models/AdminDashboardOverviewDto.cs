namespace JWTAuthAPI.Models
{
    public class AdminDashboardOverviewDto
    {
        public StudentStatistics Students { get; set; } = new();
        public CourseStatistics Courses { get; set; } = new();
        public BatchStatistics Batches { get; set; } = new();
        public StaffStatistics Staff { get; set; } = new();
        public InquiryStatistics Inquiries { get; set; } = new();
        public InventoryStatistics Inventory { get; set; } = new();
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

    public class InventoryStatistics
    {
        // Product metrics
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int OutOfStock { get; set; }
        public int LowStock { get; set; }

        // Order metrics
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ContactedOrders { get; set; }
        public int ConfirmedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int OrdersToday { get; set; }
        public int OrdersThisWeek { get; set; }
        public int OrdersThisMonth { get; set; }

        // Revenue metrics
        public decimal TotalRevenue { get; set; }
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisWeek { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal AverageOrderValue { get; set; }

        // Review metrics
        public int TotalReviews { get; set; }
        public int PendingReviews { get; set; }
        public int ApprovedReviews { get; set; }
        public double AverageRating { get; set; }
    }
}
