using JWTAuthAPI.Data;
using JWTAuthAPI.Helpers;
using JWTAuthAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace JWTAuthAPI.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<AttendanceService> _logger;

        public AttendanceService(
            ApplicationDbContext context,
            IAuditService auditService,
            ILogger<AttendanceService> logger)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<ApiResponse<AttendanceResponseDto>> MarkAttendanceAsync(MarkAttendanceDto dto, string markedBy)
        {
            try
            {
                // Validate date - cannot mark attendance for future dates
                if (dto.AttendanceDate.Date > DateTime.UtcNow.Date)
                {
                    return ResponseHelper.Error<AttendanceResponseDto>("Cannot mark attendance for future dates");
                }

                // Check if student exists
                var student = await _context.Students.FindAsync(dto.StudentId);
                if (student == null)
                {
                    return ResponseHelper.Error<AttendanceResponseDto>("Student not found");
                }

                // Check if batch exists
                var batch = await _context.Batches.FindAsync(dto.BatchId);
                if (batch == null)
                {
                    return ResponseHelper.Error<AttendanceResponseDto>("Batch not found");
                }

                // Check if student is assigned to this batch
                if (student.BatchId != dto.BatchId)
                {
                    return ResponseHelper.Error<AttendanceResponseDto>("Student is not assigned to this batch");
                }

                // Check for duplicate attendance
                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.StudentId == dto.StudentId
                                           && a.BatchId == dto.BatchId
                                           && a.AttendanceDate.Date == dto.AttendanceDate.Date);

                if (existingAttendance != null)
                {
                    return ResponseHelper.Error<AttendanceResponseDto>("Attendance already marked for this student on this date");
                }

                // Create new attendance record
                var attendance = new Attendance
                {
                    StudentId = dto.StudentId,
                    BatchId = dto.BatchId,
                    AttendanceDate = dto.AttendanceDate.Date,
                    Status = dto.Status,
                    CheckInTime = dto.CheckInTime,
                    CheckOutTime = dto.CheckOutTime,
                    Remarks = dto.Remarks,
                    MarkedBy = markedBy,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();

                // Load navigation properties
                await _context.Entry(attendance).Reference(a => a.Student).LoadAsync();
                await _context.Entry(attendance).Reference(a => a.Batch).LoadAsync();

                // Log the action
                await _auditService.LogAsync(
                    ActionType.CREATE,
                    "Attendance",
                    attendance.AttendanceId.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(attendance),
                    $"Attendance marked for student {student.Name} in batch {batch.Name}"
                );

                var response = MapToResponseDto(attendance);
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking attendance");
                return ResponseHelper.Error<AttendanceResponseDto>("An error occurred while marking attendance");
            }
        }

        public async Task<ApiResponse<List<AttendanceResponseDto>>> MarkBulkAttendanceAsync(BulkAttendanceDto dto, string markedBy)
        {
            try
            {
                // Validate date
                if (dto.AttendanceDate.Date > DateTime.UtcNow.Date)
                {
                    return ResponseHelper.Error<List<AttendanceResponseDto>>("Cannot mark attendance for future dates");
                }

                // Check if batch exists
                var batch = await _context.Batches.FindAsync(dto.BatchId);
                if (batch == null)
                {
                    return ResponseHelper.Error<List<AttendanceResponseDto>>("Batch not found");
                }

                var responses = new List<AttendanceResponseDto>();
                var errors = new List<string>();

                foreach (var studentDto in dto.Students)
                {
                    // Check if student exists and belongs to batch
                    var student = await _context.Students.FindAsync(studentDto.StudentId);
                    if (student == null)
                    {
                        errors.Add($"Student ID {studentDto.StudentId} not found");
                        continue;
                    }

                    if (student.BatchId != dto.BatchId)
                    {
                        errors.Add($"Student {student.Name} is not assigned to this batch");
                        continue;
                    }

                    // Check for duplicate
                    var existingAttendance = await _context.Attendances
                        .FirstOrDefaultAsync(a => a.StudentId == studentDto.StudentId
                                               && a.BatchId == dto.BatchId
                                               && a.AttendanceDate.Date == dto.AttendanceDate.Date);

                    if (existingAttendance != null)
                    {
                        errors.Add($"Attendance already marked for student {student.Name}");
                        continue;
                    }

                    // Create attendance record
                    var attendance = new Attendance
                    {
                        StudentId = studentDto.StudentId,
                        BatchId = dto.BatchId,
                        AttendanceDate = dto.AttendanceDate.Date,
                        Status = studentDto.Status,
                        CheckInTime = studentDto.CheckInTime,
                        CheckOutTime = studentDto.CheckOutTime,
                        Remarks = studentDto.Remarks,
                        MarkedBy = markedBy,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Attendances.Add(attendance);
                    await _context.SaveChangesAsync();

                    // Load navigation properties
                    await _context.Entry(attendance).Reference(a => a.Student).LoadAsync();
                    await _context.Entry(attendance).Reference(a => a.Batch).LoadAsync();

                    responses.Add(MapToResponseDto(attendance));
                }

                // Log bulk action
                await _auditService.LogAsync(
                    ActionType.CREATE,
                    "Attendance",
                    dto.BatchId.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(new { BatchId = dto.BatchId, Date = dto.AttendanceDate, Count = responses.Count }),
                    $"Bulk attendance marked for {responses.Count} students in batch {batch.Name}"
                );

                if (errors.Any())
                {
                    _logger.LogWarning($"Bulk attendance marked with errors: {string.Join("; ", errors)}");
                }

                return ResponseHelper.Success(responses, errors.Any() ? string.Join("; ", errors) : string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking bulk attendance");
                return ResponseHelper.Error<List<AttendanceResponseDto>>("An error occurred while marking bulk attendance");
            }
        }

        public async Task<ApiResponse<List<AttendanceResponseDto>>> GetAttendanceByStudentAsync(int studentId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.Attendances
                    .Include(a => a.Student)
                    .Include(a => a.Batch)
                    .Where(a => a.StudentId == studentId);

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.AttendanceDate.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.AttendanceDate.Date <= endDate.Value.Date);
                }

                var attendances = await query
                    .OrderByDescending(a => a.AttendanceDate)
                    .ToListAsync();

                var response = attendances.Select(MapToResponseDto).ToList();
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student attendance");
                return ResponseHelper.Error<List<AttendanceResponseDto>>("An error occurred while retrieving attendance records");
            }
        }

        public async Task<ApiResponse<List<AttendanceResponseDto>>> GetAttendanceByBatchAsync(int batchId, DateTime date)
        {
            try
            {
                var attendances = await _context.Attendances
                    .Include(a => a.Student)
                    .Include(a => a.Batch)
                    .Where(a => a.BatchId == batchId && a.AttendanceDate.Date == date.Date)
                    .OrderBy(a => a.Student!.Name)
                    .ToListAsync();

                var response = attendances.Select(MapToResponseDto).ToList();
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch attendance");
                return ResponseHelper.Error<List<AttendanceResponseDto>>("An error occurred while retrieving batch attendance");
            }
        }

        public async Task<ApiResponse<List<AttendanceResponseDto>>> GetAttendanceByBatchRangeAsync(int batchId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var attendances = await _context.Attendances
                    .Include(a => a.Student)
                    .Include(a => a.Batch)
                    .Where(a => a.BatchId == batchId
                             && a.AttendanceDate.Date >= startDate.Date
                             && a.AttendanceDate.Date <= endDate.Date)
                    .OrderByDescending(a => a.AttendanceDate)
                    .ThenBy(a => a.Student!.Name)
                    .ToListAsync();

                var response = attendances.Select(MapToResponseDto).ToList();
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch attendance range");
                return ResponseHelper.Error<List<AttendanceResponseDto>>("An error occurred while retrieving batch attendance");
            }
        }

        public async Task<ApiResponse<AttendanceResponseDto>> GetAttendanceByIdAsync(int attendanceId)
        {
            try
            {
                var attendance = await _context.Attendances
                    .Include(a => a.Student)
                    .Include(a => a.Batch)
                    .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);

                if (attendance == null)
                {
                    return ResponseHelper.Error<AttendanceResponseDto>("Attendance record not found");
                }

                var response = MapToResponseDto(attendance);
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance by ID");
                return ResponseHelper.Error<AttendanceResponseDto>("An error occurred while retrieving attendance");
            }
        }

        public async Task<ApiResponse<AttendanceResponseDto>> UpdateAttendanceAsync(int attendanceId, UpdateAttendanceDto dto, string updatedBy)
        {
            try
            {
                var attendance = await _context.Attendances
                    .Include(a => a.Student)
                    .Include(a => a.Batch)
                    .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);

                if (attendance == null)
                {
                    return ResponseHelper.Error<AttendanceResponseDto>("Attendance record not found");
                }

                var oldData = System.Text.Json.JsonSerializer.Serialize(attendance);

                // Update fields
                attendance.Status = dto.Status;
                attendance.CheckInTime = dto.CheckInTime;
                attendance.CheckOutTime = dto.CheckOutTime;
                attendance.Remarks = dto.Remarks;
                attendance.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log the update
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Attendance",
                    attendance.AttendanceId.ToString(),
                    oldData,
                    System.Text.Json.JsonSerializer.Serialize(attendance),
                    $"Attendance updated for student {attendance.Student?.Name}"
                );

                var response = MapToResponseDto(attendance);
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating attendance");
                return ResponseHelper.Error<AttendanceResponseDto>("An error occurred while updating attendance");
            }
        }

        public async Task<ApiResponse<string>> DeleteAttendanceAsync(int attendanceId, string deletedBy)
        {
            try
            {
                var attendance = await _context.Attendances
                    .Include(a => a.Student)
                    .Include(a => a.Batch)
                    .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);

                if (attendance == null)
                {
                    return ResponseHelper.Error<string>("Attendance record not found");
                }

                var oldData = System.Text.Json.JsonSerializer.Serialize(attendance);

                _context.Attendances.Remove(attendance);
                await _context.SaveChangesAsync();

                // Log the deletion
                await _auditService.LogAsync(
                    ActionType.DELETE,
                    "Attendance",
                    attendanceId.ToString(),
                    oldData,
                    null,
                    $"Attendance deleted for student {attendance.Student?.Name} on {attendance.AttendanceDate:yyyy-MM-dd}"
                );

                return ResponseHelper.Success("Attendance record deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attendance");
                return ResponseHelper.Error<string>("An error occurred while deleting attendance");
            }
        }

        public async Task<ApiResponse<AttendanceReportDto>> GetStudentAttendanceReportAsync(int studentId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                {
                    return ResponseHelper.Error<AttendanceReportDto>("Student not found");
                }

                var query = _context.Attendances
                    .Where(a => a.StudentId == studentId);

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.AttendanceDate.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.AttendanceDate.Date <= endDate.Value.Date);
                }

                var attendances = await query.ToListAsync();

                var report = new AttendanceReportDto
                {
                    EntityId = studentId,
                    EntityName = student.Name,
                    EntityType = "Student",
                    StartDate = startDate ?? (attendances.Any() ? attendances.Min(a => a.AttendanceDate) : DateTime.UtcNow),
                    EndDate = endDate ?? DateTime.UtcNow,
                    TotalRecords = attendances.Count,
                    PresentCount = attendances.Count(a => a.Status == AttendanceStatus.Present),
                    AbsentCount = attendances.Count(a => a.Status == AttendanceStatus.Absent),
                    LateCount = attendances.Count(a => a.Status == AttendanceStatus.Late),
                    ExcusedCount = attendances.Count(a => a.Status == AttendanceStatus.Excused),
                    AttendancePercentage = attendances.Any()
                        ? Math.Round((double)attendances.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late) / attendances.Count * 100, 2)
                        : 0
                };

                return ResponseHelper.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating student attendance report");
                return ResponseHelper.Error<AttendanceReportDto>("An error occurred while generating the report");
            }
        }

        public async Task<ApiResponse<AttendanceReportDto>> GetBatchAttendanceReportAsync(int batchId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var batch = await _context.Batches.FindAsync(batchId);
                if (batch == null)
                {
                    return ResponseHelper.Error<AttendanceReportDto>("Batch not found");
                }

                var query = _context.Attendances
                    .Where(a => a.BatchId == batchId);

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.AttendanceDate.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.AttendanceDate.Date <= endDate.Value.Date);
                }

                var attendances = await query.ToListAsync();

                var report = new AttendanceReportDto
                {
                    EntityId = batchId,
                    EntityName = batch.Name,
                    EntityType = "Batch",
                    StartDate = startDate ?? (attendances.Any() ? attendances.Min(a => a.AttendanceDate) : DateTime.UtcNow),
                    EndDate = endDate ?? DateTime.UtcNow,
                    TotalRecords = attendances.Count,
                    PresentCount = attendances.Count(a => a.Status == AttendanceStatus.Present),
                    AbsentCount = attendances.Count(a => a.Status == AttendanceStatus.Absent),
                    LateCount = attendances.Count(a => a.Status == AttendanceStatus.Late),
                    ExcusedCount = attendances.Count(a => a.Status == AttendanceStatus.Excused),
                    AttendancePercentage = attendances.Any()
                        ? Math.Round((double)attendances.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late) / attendances.Count * 100, 2)
                        : 0
                };

                return ResponseHelper.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating batch attendance report");
                return ResponseHelper.Error<AttendanceReportDto>("An error occurred while generating the report");
            }
        }

        // Helper method to map entity to DTO
        private AttendanceResponseDto MapToResponseDto(Attendance attendance)
        {
            return new AttendanceResponseDto
            {
                AttendanceId = attendance.AttendanceId,
                StudentId = attendance.StudentId,
                StudentName = attendance.Student?.Name ?? "Unknown",
                BatchId = attendance.BatchId,
                BatchName = attendance.Batch?.Name ?? "Unknown",
                AttendanceDate = attendance.AttendanceDate,
                Status = attendance.Status,
                StatusText = attendance.Status.ToString(),
                CheckInTime = attendance.CheckInTime,
                CheckOutTime = attendance.CheckOutTime,
                Remarks = attendance.Remarks,
                MarkedBy = attendance.MarkedBy,
                CreatedAt = attendance.CreatedAt,
                UpdatedAt = attendance.UpdatedAt
            };
        }
    }
}
