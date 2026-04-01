using JWTAuthAPI.Helpers;
using JWTAuthAPI.Models;
using JWTAuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JWTAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IDashboardService dashboardService,
            ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Get operational dashboard for staff/trainer users.
        /// Includes student, batch, inquiry, attendance, and payment-due snapshots.
        /// </summary>
        [HttpGet("staff/overview")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff},{Roles.Trainer}")]
        public async Task<ActionResult<ApiResponse<StaffDashboardOverviewDto>>> GetStaffOverview([FromQuery] int limit = 5)
        {
            try
            {
                if (limit < 1 || limit > 20)
                {
                    return BadRequest(ResponseHelper.Error<StaffDashboardOverviewDto>("Limit must be between 1 and 20", 400));
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ResponseHelper.Error<StaffDashboardOverviewDto>("User ID not found in token", 401));
                }

                var role = User.FindFirst(ClaimTypes.Role)?.Value
                           ?? User.FindFirst("role")?.Value
                           ?? string.Empty;

                var result = await _dashboardService.GetStaffOverviewAsync(userId, role, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetStaffOverview endpoint");
                return StatusCode(500, ResponseHelper.Error<StaffDashboardOverviewDto>("An error occurred while fetching staff dashboard data", 500));
            }
        }

        /// <summary>
        /// Get quick-action counts for staff/trainer dashboard cards.
        /// </summary>
        [HttpGet("staff/quick-actions")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff},{Roles.Trainer}")]
        public async Task<ActionResult<ApiResponse<StaffQuickActionsDto>>> GetStaffQuickActions()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ResponseHelper.Error<StaffQuickActionsDto>("User ID not found in token", 401));
                }

                var role = User.FindFirst(ClaimTypes.Role)?.Value
                           ?? User.FindFirst("role")?.Value
                           ?? string.Empty;

                var result = await _dashboardService.GetStaffQuickActionsAsync(userId, role);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetStaffQuickActions endpoint");
                return StatusCode(500, ResponseHelper.Error<StaffQuickActionsDto>("An error occurred while fetching quick actions", 500));
            }
        }

        /// <summary>
        /// Get recent operational timeline events for staff/trainer dashboard.
        /// </summary>
        /// <param name="limit">Maximum number of events (default: 20, max: 100)</param>
        [HttpGet("staff/timeline")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff},{Roles.Trainer}")]
        public async Task<ActionResult<ApiResponse<List<StaffTimelineItemDto>>>> GetStaffTimeline([FromQuery] int limit = 20)
        {
            try
            {
                if (limit < 1 || limit > 100)
                {
                    return BadRequest(ResponseHelper.Error<List<StaffTimelineItemDto>>("Limit must be between 1 and 100", 400));
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ResponseHelper.Error<List<StaffTimelineItemDto>>("User ID not found in token", 401));
                }

                var role = User.FindFirst(ClaimTypes.Role)?.Value
                           ?? User.FindFirst("role")?.Value
                           ?? string.Empty;

                var result = await _dashboardService.GetStaffTimelineAsync(userId, role, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetStaffTimeline endpoint");
                return StatusCode(500, ResponseHelper.Error<List<StaffTimelineItemDto>>("An error occurred while fetching timeline", 500));
            }
        }

        /// <summary>
        /// Get trainer dashboard overview scoped to trainer-assigned batches.
        /// </summary>
        [HttpGet("trainer/overview")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff},{Roles.Trainer}")]
        public async Task<ActionResult<ApiResponse<TrainerDashboardOverviewDto>>> GetTrainerOverview([FromQuery] int limit = 5)
        {
            try
            {
                if (limit < 1 || limit > 20)
                {
                    return BadRequest(ResponseHelper.Error<TrainerDashboardOverviewDto>("Limit must be between 1 and 20", 400));
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ResponseHelper.Error<TrainerDashboardOverviewDto>("User ID not found in token", 401));
                }

                var role = User.FindFirst(ClaimTypes.Role)?.Value
                           ?? User.FindFirst("role")?.Value
                           ?? string.Empty;

                var result = await _dashboardService.GetTrainerOverviewAsync(userId, role, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTrainerOverview endpoint");
                return StatusCode(500, ResponseHelper.Error<TrainerDashboardOverviewDto>("An error occurred while fetching trainer dashboard data", 500));
            }
        }

        /// <summary>
        /// Get quick-action counts for trainer dashboard cards.
        /// </summary>
        [HttpGet("trainer/quick-actions")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff},{Roles.Trainer}")]
        public async Task<ActionResult<ApiResponse<TrainerQuickActionsDto>>> GetTrainerQuickActions()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ResponseHelper.Error<TrainerQuickActionsDto>("User ID not found in token", 401));
                }

                var role = User.FindFirst(ClaimTypes.Role)?.Value
                           ?? User.FindFirst("role")?.Value
                           ?? string.Empty;

                var result = await _dashboardService.GetTrainerQuickActionsAsync(userId, role);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTrainerQuickActions endpoint");
                return StatusCode(500, ResponseHelper.Error<TrainerQuickActionsDto>("An error occurred while fetching trainer quick actions", 500));
            }
        }

        /// <summary>
        /// Get recent trainer timeline events based on assigned batches.
        /// </summary>
        /// <param name="limit">Maximum number of events (default: 20, max: 100)</param>
        [HttpGet("trainer/timeline")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff},{Roles.Trainer}")]
        public async Task<ActionResult<ApiResponse<List<TrainerTimelineItemDto>>>> GetTrainerTimeline([FromQuery] int limit = 20)
        {
            try
            {
                if (limit < 1 || limit > 100)
                {
                    return BadRequest(ResponseHelper.Error<List<TrainerTimelineItemDto>>("Limit must be between 1 and 100", 400));
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ResponseHelper.Error<List<TrainerTimelineItemDto>>("User ID not found in token", 401));
                }

                var role = User.FindFirst(ClaimTypes.Role)?.Value
                           ?? User.FindFirst("role")?.Value
                           ?? string.Empty;

                var result = await _dashboardService.GetTrainerTimelineAsync(userId, role, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTrainerTimeline endpoint");
                return StatusCode(500, ResponseHelper.Error<List<TrainerTimelineItemDto>>("An error occurred while fetching trainer timeline", 500));
            }
        }

        /// <summary>
        /// Get admin dashboard overview with statistics on students, courses, batches, staff, and inquiries
        /// </summary>
        [HttpGet("overview")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<AdminDashboardOverviewDto>>> GetOverview()
        {
            try
            {
                var result = await _dashboardService.GetAdminOverviewAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOverview endpoint");
                return StatusCode(500, ResponseHelper.Error<AdminDashboardOverviewDto>("An error occurred while fetching overview data", 500));
            }
        }

        /// <summary>
        /// Get financial summary including revenue, outstanding amounts, collection metrics, and upcoming payments
        /// </summary>
        [HttpGet("financial")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<AdminFinancialSummaryDto>>> GetFinancialSummary()
        {
            try
            {
                var result = await _dashboardService.GetFinancialSummaryAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFinancialSummary endpoint");
                return StatusCode(500, ResponseHelper.Error<AdminFinancialSummaryDto>("An error occurred while fetching financial data", 500));
            }
        }

        /// <summary>
        /// Get recent activities including new students, recent payments, inquiries, and upcoming batches
        /// </summary>
        /// <param name="limit">Number of records to return per category (default: 10)</param>
        [HttpGet("activities")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<AdminRecentActivitiesDto>>> GetRecentActivities([FromQuery] int limit = 10)
        {
            try
            {
                if (limit < 1 || limit > 50)
                {
                    return BadRequest(ResponseHelper.Error<AdminRecentActivitiesDto>("Limit must be between 1 and 50", 400));
                }

                var result = await _dashboardService.GetRecentActivitiesAsync(limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecentActivities endpoint");
                return StatusCode(500, ResponseHelper.Error<AdminRecentActivitiesDto>("An error occurred while fetching recent activities", 500));
            }
        }

        /// <summary>
        /// Get system alerts including payment, attendance, inquiry, and batch alerts
        /// </summary>
        [HttpGet("alerts")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<AdminAlertsDto>>> GetAlerts()
        {
            try
            {
                var result = await _dashboardService.GetAlertsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAlerts endpoint");
                return StatusCode(500, ResponseHelper.Error<AdminAlertsDto>("An error occurred while fetching alerts", 500));
            }
        }

        /// <summary>
        /// Get chart data for visualizations including revenue trends, enrollment, and payment collection
        /// </summary>
        /// <param name="months">Number of months to include in historical data (default: 6)</param>
        [HttpGet("charts")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<AdminChartsDto>>> GetChartsData([FromQuery] int months = 6)
        {
            try
            {
                if (months < 1 || months > 24)
                {
                    return BadRequest(ResponseHelper.Error<AdminChartsDto>("Months must be between 1 and 24", 400));
                }

                var result = await _dashboardService.GetChartsDataAsync(months);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetChartsData endpoint");
                return StatusCode(500, ResponseHelper.Error<AdminChartsDto>("An error occurred while fetching chart data", 500));
            }
        }

        /// <summary>
        /// Get attendance analytics including today's attendance, weekly/monthly rates, and batch-wise breakdown
        /// </summary>
        [HttpGet("attendance")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<AdminAttendanceAnalyticsDto>>> GetAttendanceAnalytics()
        {
            try
            {
                var result = await _dashboardService.GetAttendanceAnalyticsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAttendanceAnalytics endpoint");
                return StatusCode(500, ResponseHelper.Error<AdminAttendanceAnalyticsDto>("An error occurred while fetching attendance analytics", 500));
            }
        }

        /// <summary>
        /// Global search for header search bar (students, courses, batches, inquiries)
        /// </summary>
        /// <param name="q">Search text</param>
        /// <param name="limit">Maximum number of results (default: 15, max: 25)</param>
        [HttpGet("search")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff},{Roles.Trainer}")]
        public async Task<ActionResult<ApiResponse<AdminGlobalSearchDto>>> Search([FromQuery] string q, [FromQuery] int limit = 15)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 1)
                {
                    return BadRequest(ResponseHelper.Error<AdminGlobalSearchDto>("Search query is required", 400));
                }

                if (limit < 1 || limit > 25)
                {
                    return BadRequest(ResponseHelper.Error<AdminGlobalSearchDto>("Limit must be between 1 and 25", 400));
                }

                var result = await _dashboardService.SearchAsync(q, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Search endpoint");
                return StatusCode(500, ResponseHelper.Error<AdminGlobalSearchDto>("An error occurred while searching", 500));
            }
        }

        /// <summary>
        /// Get notifications for bell icon including payment alerts, new inquiries, attendance issues, and recent activities
        /// </summary>
        /// <param name="limit">Maximum number of notifications to return (default: 50, max: 100)</param>
        [HttpGet("notifications")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff},{Roles.Trainer}")]
        public async Task<ActionResult<ApiResponse<NotificationResponseDto>>> GetNotifications([FromQuery] int limit = 50)
        {
            try
            {
                if (limit < 1 || limit > 100)
                {
                    return BadRequest(ResponseHelper.Error<NotificationResponseDto>("Limit must be between 1 and 100", 400));
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ResponseHelper.Error<NotificationResponseDto>("User ID not found in token", 401));
                }

                var result = await _dashboardService.GetNotificationsAsync(userId, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNotifications endpoint");
                return StatusCode(500, ResponseHelper.Error<NotificationResponseDto>("An error occurred while fetching notifications", 500));
            }
        }

        /// <summary>
        /// Mark a single notification as read
        /// </summary>
        /// <param name="request">Request containing the notification key to mark as read</param>
        [HttpPost("notifications/mark-read")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff},{Roles.Trainer}")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkNotificationAsRead([FromBody] MarkNotificationReadDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.NotificationKey))
                {
                    return BadRequest(ResponseHelper.Error<bool>("Notification key is required", 400));
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ResponseHelper.Error<bool>("User ID not found in token", 401));
                }

                var result = await _dashboardService.MarkNotificationAsReadAsync(userId, request.NotificationKey);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MarkNotificationAsRead endpoint");
                return StatusCode(500, ResponseHelper.Error<bool>("An error occurred while marking notification as read", 500));
            }
        }

        /// <summary>
        /// Mark all notifications as read for the current user
        /// </summary>
        [HttpPost("notifications/mark-all-read")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff},{Roles.Trainer}")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAllNotificationsAsRead()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ResponseHelper.Error<bool>("User ID not found in token", 401));
                }

                var result = await _dashboardService.MarkAllNotificationsAsReadAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MarkAllNotificationsAsRead endpoint");
                return StatusCode(500, ResponseHelper.Error<bool>("An error occurred while marking all notifications as read", 500));
            }
        }
    }
}
