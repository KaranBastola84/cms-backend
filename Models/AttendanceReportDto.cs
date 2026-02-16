namespace JWTAuthAPI.Models
{
    public class AttendanceReportDto
    {
        public int EntityId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalRecords { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public int ExcusedCount { get; set; }
        public double AttendancePercentage { get; set; }
    }

    public class BatchAttendanceReportDto
    {
        public int BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<AttendanceReportDto> StudentReports { get; set; } = new List<AttendanceReportDto>();
    }

    public class AttendanceStatisticsDto
    {
        public int TotalClasses { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public int ExcusedCount { get; set; }
        public double AttendancePercentage { get; set; }
    }
}
