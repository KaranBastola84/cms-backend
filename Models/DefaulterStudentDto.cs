namespace JWTAuthAPI.Models
{
    public class DefaulterStudentDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string? StudentPhone { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int PaymentPlanId { get; set; }
        public decimal TotalOverdueAmount { get; set; }
        public int OverdueInstallments { get; set; }
        public int OldestOverdueDays { get; set; }
        public DateTime? LastPaymentDate { get; set; }
    }
}
