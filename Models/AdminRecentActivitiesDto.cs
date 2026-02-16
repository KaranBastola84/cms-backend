namespace JWTAuthAPI.Models
{
    public class AdminRecentActivitiesDto
    {
        public List<RecentStudentDto> RecentStudents { get; set; } = new();
        public List<RecentPaymentDto> RecentPayments { get; set; } = new();
        public List<RecentInquiryDto> RecentInquiries { get; set; } = new();
        public List<RecentBatchDto> UpcomingBatches { get; set; } = new();
    }

    public class RecentStudentDto
    {
        public int StudentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? CourseName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class RecentPaymentDto
    {
        public int ReceiptId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
    }

    public class RecentInquiryDto
    {
        public int InquiryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? CourseName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class RecentBatchDto
    {
        public int BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public int EnrolledStudents { get; set; }
        public int Capacity { get; set; }
    }
}
