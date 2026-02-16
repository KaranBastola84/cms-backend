using JWTAuthAPI.Models;

namespace JWTAuthAPI.Services
{
    public interface IPaymentPlanService
    {
        Task<ApiResponse<PaymentPlanResponseDto>> CreatePaymentPlanAsync(CreatePaymentPlanDto dto, string createdBy);
        Task<ApiResponse<PaymentPlanResponseDto>> GetPaymentPlanByIdAsync(int paymentPlanId);
        Task<ApiResponse<List<PaymentPlanResponseDto>>> GetPaymentPlansByStudentIdAsync(int studentId);
        Task<ApiResponse<List<PaymentPlanResponseDto>>> GetPaymentPlansByCourseIdAsync(int courseId);
        Task<ApiResponse<PaymentPlanResponseDto>> UpdatePaymentPlanStatusAsync(int paymentPlanId, PaymentPlanStatus status, string updatedBy);
        Task<ApiResponse<InstallmentResponseDto>> GetInstallmentByIdAsync(int installmentId);
        Task<ApiResponse<InstallmentResponseDto>> PayInstallmentAsync(int installmentId, PayInstallmentDto dto, string processedBy);
        Task<ApiResponse<List<InstallmentResponseDto>>> GetOverdueInstallmentsAsync(int? days);
        Task<ApiResponse<List<InstallmentResponseDto>>> GetUpcomingInstallmentsAsync(int days = 7);
    }
}
