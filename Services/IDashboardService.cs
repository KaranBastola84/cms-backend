using JWTAuthAPI.Models;

namespace JWTAuthAPI.Services
{
    public interface IDashboardService
    {
        Task<ApiResponse<AdminDashboardOverviewDto>> GetAdminOverviewAsync();
        Task<ApiResponse<AdminFinancialSummaryDto>> GetFinancialSummaryAsync();
        Task<ApiResponse<AdminRecentActivitiesDto>> GetRecentActivitiesAsync(int limit = 10);
        Task<ApiResponse<AdminAlertsDto>> GetAlertsAsync();
        Task<ApiResponse<AdminChartsDto>> GetChartsDataAsync(int months = 6);
        Task<ApiResponse<AdminAttendanceAnalyticsDto>> GetAttendanceAnalyticsAsync();
        Task<ApiResponse<NotificationResponseDto>> GetNotificationsAsync(int limit = 50);
    }
}
