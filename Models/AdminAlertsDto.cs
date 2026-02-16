namespace JWTAuthAPI.Models
{
    public class AdminAlertsDto
    {
        public List<PaymentAlertDto> PaymentAlerts { get; set; } = new();
        public List<AttendanceAlertDto> AttendanceAlerts { get; set; } = new();
        public List<InquiryAlertDto> InquiryAlerts { get; set; } = new();
        public List<BatchAlertDto> BatchAlerts { get; set; } = new();
        public int TotalCriticalAlerts { get; set; }
        public int TotalWarningAlerts { get; set; }
    }

    public class PaymentAlertDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public decimal OverdueAmount { get; set; }
        public int OverdueDays { get; set; }
        public int OverdueInstallments { get; set; }
        public string Severity { get; set; } = "Warning"; // Critical, Warning, Info
    }

    public class AttendanceAlertDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;
        public int ConsecutiveAbsences { get; set; }
        public decimal AttendancePercentage { get; set; }
        public string Severity { get; set; } = "Warning";
    }

    public class InquiryAlertDto
    {
        public int InquiryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int DaysSinceInquiry { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class BatchAlertDto
    {
        public int BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public int DaysUntilStart { get; set; }
        public int EnrolledStudents { get; set; }
        public int Capacity { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
