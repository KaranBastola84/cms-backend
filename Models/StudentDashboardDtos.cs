namespace JWTAuthAPI.Models
{
    public class StudentDashboardOverviewDto
    {
        public StudentDashboardProfileDto Profile { get; set; } = new();
        public StudentDashboardAttendanceDto Attendance { get; set; } = new();
        public StudentDashboardFinanceDto Finance { get; set; } = new();
        public List<StudentUpcomingInstallmentDto> UpcomingInstallments { get; set; } = new();
        public List<StudentRecentReceiptDto> RecentReceipts { get; set; } = new();
        public List<StudentRecentAttendanceDto> RecentAttendance { get; set; } = new();
    }

    public class StudentDashboardProfileDto
    {
        public int StudentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? CourseName { get; set; }
        public string? BatchName { get; set; }
        public string? BatchTimeSlot { get; set; }
    }

    public class StudentDashboardAttendanceDto
    {
        public int TotalMarkedLast30Days { get; set; }
        public int PresentLast30Days { get; set; }
        public int AbsentLast30Days { get; set; }
        public int LateLast30Days { get; set; }
        public decimal AttendanceRateLast30Days { get; set; }
        public string? TodayStatus { get; set; }
    }

    public class StudentDashboardFinanceDto
    {
        public decimal TotalPlanAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalBalanceAmount { get; set; }
        public int PendingInstallments { get; set; }
        public int OverdueInstallments { get; set; }
        public int InstallmentsDueNext7Days { get; set; }
        public decimal? NextDueAmount { get; set; }
        public DateTime? NextDueDate { get; set; }
    }

    public class StudentUpcomingInstallmentDto
    {
        public int InstallmentId { get; set; }
        public int InstallmentNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public int DaysUntilDue { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class StudentRecentReceiptDto
    {
        public int ReceiptId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string ReceiptType { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public DateTime IssuedAt { get; set; }
    }

    public class StudentRecentAttendanceDto
    {
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? BatchName { get; set; }
        public string? Remarks { get; set; }
    }

    public class StudentQuickActionsDto
    {
        public int PendingInstallments { get; set; }
        public int OverdueInstallments { get; set; }
        public int InstallmentsDueNext7Days { get; set; }
        public int DocumentsUploaded { get; set; }
        public int ReceiptsThisMonth { get; set; }
        public decimal AttendanceRateLast30Days { get; set; }
    }

    public class StudentTimelineItemDto
    {
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? ActionUrl { get; set; }
    }
}
