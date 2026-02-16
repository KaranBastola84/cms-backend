using JWTAuthAPI.Models;

namespace JWTAuthAPI.Services
{
    public interface IAttendanceService
    {
        // Mark attendance
        Task<ApiResponse<AttendanceResponseDto>> MarkAttendanceAsync(MarkAttendanceDto dto, string markedBy);
        Task<ApiResponse<List<AttendanceResponseDto>>> MarkBulkAttendanceAsync(BulkAttendanceDto dto, string markedBy);

        // Get attendance records
        Task<ApiResponse<List<AttendanceResponseDto>>> GetAttendanceByStudentAsync(int studentId, DateTime? startDate = null, DateTime? endDate = null);
        Task<ApiResponse<List<AttendanceResponseDto>>> GetAttendanceByBatchAsync(int batchId, DateTime date);
        Task<ApiResponse<List<AttendanceResponseDto>>> GetAttendanceByBatchRangeAsync(int batchId, DateTime startDate, DateTime endDate);
        Task<ApiResponse<AttendanceResponseDto>> GetAttendanceByIdAsync(int attendanceId);

        // Update and delete
        Task<ApiResponse<AttendanceResponseDto>> UpdateAttendanceAsync(int attendanceId, UpdateAttendanceDto dto, string updatedBy);
        Task<ApiResponse<string>> DeleteAttendanceAsync(int attendanceId, string deletedBy);

        // Statistics and reports
        Task<ApiResponse<AttendanceReportDto>> GetStudentAttendanceReportAsync(int studentId, DateTime? startDate = null, DateTime? endDate = null);
        Task<ApiResponse<AttendanceReportDto>> GetBatchAttendanceReportAsync(int batchId, DateTime? startDate = null, DateTime? endDate = null);
    }
}
