using JWTAuthAPI.Models;
using JWTAuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JWTAuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Staff")]
    public class FinancialReportController : ControllerBase
    {
        private readonly IFinancialReportService _financialReportService;

        public FinancialReportController(IFinancialReportService financialReportService)
        {
            _financialReportService = financialReportService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetFinancialSummary()
        {
            var result = await _financialReportService.GetFinancialSummaryAsync();
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("outstanding-payments")]
        public async Task<IActionResult> GetOutstandingPayments()
        {
            var result = await _financialReportService.GetOutstandingPaymentsAsync();
            return Ok(result);
        }

        [HttpGet("defaulters")]
        public async Task<IActionResult> GetDefaulters([FromQuery] int overdueThresholdDays = 7)
        {
            var result = await _financialReportService.GetDefaultersAsync(overdueThresholdDays);
            return Ok(result);
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var result = await _financialReportService.GetRevenueReportAsync(startDate, endDate);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("course/{courseId}/revenue")]
        public async Task<IActionResult> GetCourseRevenue(
            int courseId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var result = await _financialReportService.GetCourseRevenueAsync(courseId, startDate, endDate);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }
    }
}
