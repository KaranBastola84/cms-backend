namespace JWTAuthAPI.Models
{
    public class TrainerDashboardOverviewDto
    {
        public TrainerDashboardSummaryDto Summary { get; set; } = new();
        public TrainerAttendanceSnapshotDto AttendanceToday { get; set; } = new();
        public List<TrainerBatchSnapshotDto> ActiveBatches { get; set; } = new();
        public List<TrainerUpcomingBatchDto> UpcomingBatches { get; set; } = new();
        public List<TrainerRecentStudentDto> RecentStudents { get; set; } = new();
    }

    public class TrainerDashboardSummaryDto
    {
        public int AssignedBatches { get; set; }
        public int ActiveBatches { get; set; }
        public int UpcomingBatchesNext7Days { get; set; }
        public int TotalStudentsInMyBatches { get; set; }
        public int StudentsMarkedToday { get; set; }
        public decimal AttendanceRateLast7Days { get; set; }
    }

    public class TrainerAttendanceSnapshotDto
    {
        public DateTime Date { get; set; }
        public int TotalMarked { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Late { get; set; }
        public decimal AttendanceRate { get; set; }
    }

    public class TrainerBatchSnapshotDto
    {
        public int BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public string? CourseName { get; set; }
        public string? TimeSlot { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int TotalStudents { get; set; }
        public int Capacity { get; set; }
    }

    public class TrainerUpcomingBatchDto
    {
        public int BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public string? CourseName { get; set; }
        public string? TimeSlot { get; set; }
        public DateTime StartDate { get; set; }
        public int DaysUntilStart { get; set; }
    }

    public class TrainerRecentStudentDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? BatchName { get; set; }
        public string? CourseName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class TrainerQuickActionsDto
    {
        public int ActiveBatchesToday { get; set; }
        public int UnmarkedActiveBatchesToday { get; set; }
        public int StudentsToMarkToday { get; set; }
        public int AttendanceMarkedToday { get; set; }
        public int UpcomingBatchesNext3Days { get; set; }
        public int NewStudentsThisWeek { get; set; }
    }

    public class TrainerTimelineItemDto
    {
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? ActionUrl { get; set; }
    }
}
