namespace JWTAuthAPI.Models
{
    public class StaffDashboardOverviewDto
    {
        public StaffDashboardSummaryDto Summary { get; set; } = new();
        public StaffAttendanceSnapshotDto AttendanceToday { get; set; } = new();
        public List<StaffUpcomingBatchDto> UpcomingBatches { get; set; } = new();
        public List<StaffPendingInquiryDto> PendingInquiries { get; set; } = new();
        public List<StaffPaymentDueDto> UpcomingPayments { get; set; } = new();
        public List<StaffRecentStudentDto> RecentStudents { get; set; } = new();
    }

    public class StaffDashboardSummaryDto
    {
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int ActiveBatches { get; set; }
        public int PendingInquiries { get; set; }
        public int OverdueInstallments { get; set; }
        public int PaymentsDueNext7Days { get; set; }
        public decimal OutstandingAmount { get; set; }
    }

    public class StaffAttendanceSnapshotDto
    {
        public DateTime Date { get; set; }
        public int TotalMarked { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Late { get; set; }
        public decimal AttendanceRate { get; set; }
    }

    public class StaffUpcomingBatchDto
    {
        public int BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public string? CourseName { get; set; }
        public DateTime StartDate { get; set; }
        public int DaysUntilStart { get; set; }
        public int EnrolledStudents { get; set; }
        public int Capacity { get; set; }
    }

    public class StaffPendingInquiryDto
    {
        public int InquiryId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? CourseInterest { get; set; }
        public DateTime CreatedAt { get; set; }
        public int DaysOpen { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class StaffPaymentDueDto
    {
        public int InstallmentId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public int DaysUntilDue { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class StaffRecentStudentDto
    {
        public int StudentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? CourseName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
