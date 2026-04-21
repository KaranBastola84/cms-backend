using JWTAuthAPI.Models;

namespace JWTAuthAPI.Services
{
    public interface IDashboardService
    {
        Task<ApiResponse<AdminDashboardOverviewDto>> GetAdminOverviewAsync();
        Task<ApiResponse<StaffDashboardOverviewDto>> GetStaffOverviewAsync(int userId, string role, int limit = 5);
        Task<ApiResponse<StaffQuickActionsDto>> GetStaffQuickActionsAsync(int userId, string role);
        Task<ApiResponse<List<StaffTimelineItemDto>>> GetStaffTimelineAsync(int userId, string role, int limit = 20);
        Task<ApiResponse<TrainerDashboardOverviewDto>> GetTrainerOverviewAsync(int userId, string role, int limit = 5);
        Task<ApiResponse<TrainerQuickActionsDto>> GetTrainerQuickActionsAsync(int userId, string role);
        Task<ApiResponse<List<TrainerTimelineItemDto>>> GetTrainerTimelineAsync(int userId, string role, int limit = 20);
        Task<ApiResponse<StudentDashboardOverviewDto>> GetStudentOverviewAsync(int studentId, string role, int limit = 5);
        Task<ApiResponse<StudentQuickActionsDto>> GetStudentQuickActionsAsync(int studentId, string role);
        Task<ApiResponse<List<StudentTimelineItemDto>>> GetStudentTimelineAsync(int studentId, string role, int limit = 20);
        Task<ApiResponse<AdminFinancialSummaryDto>> GetFinancialSummaryAsync();
        Task<ApiResponse<AdminRecentActivitiesDto>> GetRecentActivitiesAsync(int limit = 10);
        Task<ApiResponse<AdminAlertsDto>> GetAlertsAsync();
        Task<ApiResponse<AdminChartsDto>> GetChartsDataAsync(int months = 6);
        Task<ApiResponse<AdminAttendanceAnalyticsDto>> GetAttendanceAnalyticsAsync();
        Task<ApiResponse<AdminGlobalSearchDto>> SearchAsync(string query, int limit = 15);
        Task<ApiResponse<AdminGlobalSearchDto>> SearchStudentAsync(int studentId, string query, int limit = 15);
        Task<ApiResponse<NotificationResponseDto>> GetNotificationsAsync(int userId, int limit = 50, string role = "");
        Task<ApiResponse<bool>> MarkNotificationAsReadAsync(int userId, string notificationKey, string role = "");
        Task<ApiResponse<bool>> MarkAllNotificationsAsReadAsync(int userId, string role = "");
    }
}
