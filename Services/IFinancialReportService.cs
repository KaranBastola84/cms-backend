using JWTAuthAPI.Models;

namespace JWTAuthAPI.Services
{
    public interface IFinancialReportService
    {
        Task<ApiResponse<FinancialSummaryDto>> GetFinancialSummaryAsync();
        Task<ApiResponse<List<OutstandingPaymentDto>>> GetOutstandingPaymentsAsync();
        Task<ApiResponse<List<DefaulterStudentDto>>> GetDefaultersAsync(int overdueThresholdDays = 7);
        Task<ApiResponse<RevenueReportDto>> GetRevenueReportAsync(DateTime startDate, DateTime endDate);
        Task<ApiResponse<CourseRevenueDto>> GetCourseRevenueAsync(int courseId, DateTime? startDate, DateTime? endDate);
    }
}
