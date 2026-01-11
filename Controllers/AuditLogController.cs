using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JWTAuthAPI.Services;
using JWTAuthAPI.Models;
using JWTAuthAPI.Helpers;

namespace JWTAuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Only admins can view audit logs
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditLogController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        /// <summary>
        /// Get all audit logs (paginated)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllLogs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            var logs = await _auditService.GetLogsAsync(pageNumber, pageSize);
            return Ok(ResponseHelper.Success(logs, "Audit logs retrieved successfully"));
        }

        /// <summary>
        /// Get audit logs for a specific user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserLogs(string userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            var logs = await _auditService.GetLogsByUserAsync(userId, pageNumber, pageSize);
            return Ok(ResponseHelper.Success(logs, "User audit logs retrieved successfully"));
        }

        /// <summary>
        /// Get audit logs for a specific module
        /// </summary>
        [HttpGet("module/{module}")]
        public async Task<IActionResult> GetModuleLogs(string module, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            var logs = await _auditService.GetLogsByModuleAsync(module, pageNumber, pageSize);
            return Ok(ResponseHelper.Success(logs, $"{module} audit logs retrieved successfully"));
        }
    }
}
