namespace JWTAuthAPI.Models
{
    public class StripePaymentResponseDto
    {
        public int StripePaymentId { get; set; }
        public string PaymentIntentId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public int? InstallmentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
