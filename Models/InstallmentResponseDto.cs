namespace JWTAuthAPI.Models
{
    public class InstallmentResponseDto
    {
        public int InstallmentId { get; set; }
        public int PaymentPlanId { get; set; }
        public int InstallmentNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public InstallmentStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public int? ReceiptId { get; set; }
        public string? StripePaymentIntentId { get; set; }
        public string? Remarks { get; set; }
        public bool IsOverdue => Status == InstallmentStatus.Pending && DueDate < DateTime.UtcNow;
        public int DaysOverdue => IsOverdue ? (DateTime.UtcNow - DueDate).Days : 0;
    }
}
