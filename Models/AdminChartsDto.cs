namespace JWTAuthAPI.Models
{
    public class AdminChartsDto
    {
        public List<RevenueTrendDto> RevenueTrend { get; set; } = new();
        public List<EnrollmentByCourseDto> EnrollmentByCourse { get; set; } = new();
        public List<MonthlyAttendanceDto> MonthlyAttendance { get; set; } = new();
        public StudentStatusDistributionDto StudentStatusDistribution { get; set; } = new();
        public List<PaymentCollectionDto> PaymentCollection { get; set; } = new();
    }

    public class RevenueTrendDto
    {
        public string Month { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal Revenue { get; set; }
        public int PaymentCount { get; set; }
    }

    public class EnrollmentByCourseDto
    {
        public string CourseName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal Percentage { get; set; }
    }

    public class MonthlyAttendanceDto
    {
        public string Month { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal AttendanceRate { get; set; }
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
    }

    public class StudentStatusDistributionDto
    {
        public int PendingPayment { get; set; }
        public int Enrolled { get; set; }
        public int Active { get; set; }
        public int Completed { get; set; }
        public int Dropped { get; set; }
        public int Suspended { get; set; }
    }

    public class PaymentCollectionDto
    {
        public string Month { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal Collected { get; set; }
        public decimal Outstanding { get; set; }
        public decimal Expected { get; set; }
    }

    public class AdminAttendanceAnalyticsDto
    {
        public decimal TodayAttendanceRate { get; set; }
        public decimal ThisWeekAttendanceRate { get; set; }
        public decimal ThisMonthAttendanceRate { get; set; }
        public decimal OverallAttendanceRate { get; set; }
        public int TotalPresentToday { get; set; }
        public int TotalAbsentToday { get; set; }
        public int TotalLateToday { get; set; }
        public int StudentsWithLowAttendance { get; set; }
        public List<BatchAttendanceDto> BatchAttendance { get; set; } = new();
    }

    public class BatchAttendanceDto
    {
        public int BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public decimal AttendanceRate { get; set; }
        public int TotalStudents { get; set; }
        public int PresentToday { get; set; }
    }
}
