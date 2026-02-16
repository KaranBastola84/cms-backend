using JWTAuthAPI.Models;
using Stripe;

namespace JWTAuthAPI.Services
{
    public interface IStripePaymentService
    {
        Task<ApiResponse<StripePaymentResponseDto>> CreatePaymentIntentAsync(CreateStripePaymentDto dto, string createdBy);
        Task<ApiResponse<StripePaymentResponseDto>> GetPaymentByIdAsync(int stripePaymentId);
        Task<ApiResponse<StripePaymentResponseDto>> GetStripePaymentByIdAsync(int paymentId);
        Task<ApiResponse<List<StripePaymentResponseDto>>> GetStripePaymentsByStudentIdAsync(int studentId);
        Task<ApiResponse<StripePaymentResponseDto>> GetPaymentByIntentIdAsync(string paymentIntentId);
        Task<ApiResponse<string>> HandleWebhookAsync(Event stripeEvent);
        Task<ApiResponse<StripePaymentResponseDto>> ConfirmPaymentAsync(int paymentId);
    }
}
