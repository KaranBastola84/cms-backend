using JWTAuthAPI.Helpers;
using JWTAuthAPI.Models;
using JWTAuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JWTAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
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
        /// Get admin dashboard overview with statistics on students, courses, batches, staff, and inquiries
        /// </summary>
        [HttpGet("overview")]
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
    }
}
