using JWTAuthAPI.Models;
using JWTAuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JWTAuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        /// <summary>
        /// Mark attendance for a single student
        /// </summary>
        [HttpPost]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
        public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var markedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _attendanceService.MarkAttendanceAsync(dto, markedBy);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Mark attendance for multiple students at once
        /// </summary>
        [HttpPost("bulk")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
        public async Task<IActionResult> MarkBulkAttendance([FromBody] BulkAttendanceDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var markedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _attendanceService.MarkBulkAttendanceAsync(dto, markedBy);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get attendance records for a specific student
        /// </summary>
        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetAttendanceByStudent(
            int studentId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var result = await _attendanceService.GetAttendanceByStudentAsync(studentId, startDate, endDate);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get attendance for a batch on a specific date
        /// </summary>
        [HttpGet("batch/{batchId}")]
        public async Task<IActionResult> GetAttendanceByBatch(
            int batchId,
            [FromQuery] DateTime date)
        {
            var result = await _attendanceService.GetAttendanceByBatchAsync(batchId, date);
            return Ok(result);
        }

        /// <summary>
        /// Get attendance for a batch within a date range
        /// </summary>
        [HttpGet("batch/{batchId}/range")]
        public async Task<IActionResult> GetAttendanceByBatchRange(
            int batchId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var result = await _attendanceService.GetAttendanceByBatchRangeAsync(batchId, startDate, endDate);
            return Ok(result);
        }

        /// <summary>
        /// Get a specific attendance record by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAttendanceById(int id)
        {
            var result = await _attendanceService.GetAttendanceByIdAsync(id);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Update an existing attendance record
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
        public async Task<IActionResult> UpdateAttendance(int id, [FromBody] UpdateAttendanceDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _attendanceService.UpdateAttendanceAsync(id, dto, updatedBy);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Delete an attendance record
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            var deletedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _attendanceService.DeleteAttendanceAsync(id, deletedBy);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get attendance report/statistics for a specific student
        /// </summary>
        [HttpGet("report/student/{studentId}")]
        public async Task<IActionResult> GetStudentAttendanceReport(
            int studentId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var result = await _attendanceService.GetStudentAttendanceReportAsync(studentId, startDate, endDate);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get attendance report/statistics for a specific batch
        /// </summary>
        [HttpGet("report/batch/{batchId}")]
        public async Task<IActionResult> GetBatchAttendanceReport(
            int batchId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var result = await _attendanceService.GetBatchAttendanceReportAsync(batchId, startDate, endDate);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
    }
}
