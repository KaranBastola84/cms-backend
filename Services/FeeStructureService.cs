using JWTAuthAPI.Data;
using JWTAuthAPI.Helpers;
using JWTAuthAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace JWTAuthAPI.Services
{
    public class FeeStructureService : IFeeStructureService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<FeeStructureService> _logger;

        public FeeStructureService(
            ApplicationDbContext context,
            IAuditService auditService,
            ILogger<FeeStructureService> logger)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<ApiResponse<FeeStructureResponseDto>> CreateFeeStructureAsync(CreateFeeStructureDto dto, string createdBy)
        {
            try
            {
                // Validate course
                var course = await _context.Courses.FindAsync(dto.CourseId);
                if (course == null)
                {
                    return ResponseHelper.Error<FeeStructureResponseDto>("Course not found");
                }

                // Check for duplicate
                var existing = await _context.FeeStructures
                    .FirstOrDefaultAsync(f => f.CourseId == dto.CourseId && f.FeeType == dto.FeeType && f.IsActive);

                if (existing != null)
                {
                    return ResponseHelper.Error<FeeStructureResponseDto>("Fee structure with this type already exists for this course");
                }

                var feeStructure = new FeeStructure
                {
                    CourseId = dto.CourseId,
                    FeeType = dto.FeeType,
                    Amount = dto.Amount,
                    Description = dto.Description,
                    IsActive = true,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.FeeStructures.Add(feeStructure);
                await _context.SaveChangesAsync();

                await _context.Entry(feeStructure).Reference(f => f.Course).LoadAsync();

                // Log
                await _auditService.LogAsync(
                    ActionType.CREATE,
                    "FeeStructure",
                    feeStructure.FeeStructureId.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(feeStructure),
                    $"Fee structure created for course {course.Name}"
                );

                var response = MapToResponseDto(feeStructure);
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fee structure");
                return ResponseHelper.Error<FeeStructureResponseDto>("An error occurred while creating fee structure");
            }
        }

        public async Task<ApiResponse<FeeStructureResponseDto>> GetFeeStructureByIdAsync(int feeStructureId)
        {
            try
            {
                var feeStructure = await _context.FeeStructures
                    .Include(f => f.Course)
                    .FirstOrDefaultAsync(f => f.FeeStructureId == feeStructureId);

                if (feeStructure == null)
                {
                    return ResponseHelper.Error<FeeStructureResponseDto>("Fee structure not found");
                }

                var response = MapToResponseDto(feeStructure);
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fee structure");
                return ResponseHelper.Error<FeeStructureResponseDto>("An error occurred");
            }
        }

        public async Task<ApiResponse<List<FeeStructureResponseDto>>> GetFeeStructuresByCourseIdAsync(int courseId)
        {
            try
            {
                var feeStructures = await _context.FeeStructures
                    .Include(f => f.Course)
                    .Where(f => f.CourseId == courseId)
                    .OrderBy(f => f.FeeType)
                    .ToListAsync();

                var response = feeStructures.Select(MapToResponseDto).ToList();
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fee structures");
                return ResponseHelper.Error<List<FeeStructureResponseDto>>("An error occurred");
            }
        }

        public async Task<ApiResponse<List<FeeStructureResponseDto>>> GetAllFeeStructuresAsync()
        {
            try
            {
                var feeStructures = await _context.FeeStructures
                    .Include(f => f.Course)
                    .OrderBy(f => f.Course!.Name)
                    .ThenBy(f => f.FeeType)
                    .ToListAsync();

                var response = feeStructures.Select(MapToResponseDto).ToList();
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all fee structures");
                return ResponseHelper.Error<List<FeeStructureResponseDto>>("An error occurred");
            }
        }

        public async Task<ApiResponse<FeeStructureResponseDto>> UpdateFeeStructureAsync(int feeStructureId, UpdateFeeStructureDto dto, string updatedBy)
        {
            try
            {
                var feeStructure = await _context.FeeStructures
                    .Include(f => f.Course)
                    .FirstOrDefaultAsync(f => f.FeeStructureId == feeStructureId);

                if (feeStructure == null)
                {
                    return ResponseHelper.Error<FeeStructureResponseDto>("Fee structure not found");
                }

                var oldData = System.Text.Json.JsonSerializer.Serialize(feeStructure);

                if (!string.IsNullOrWhiteSpace(dto.FeeType))
                    feeStructure.FeeType = dto.FeeType;

                if (dto.Amount.HasValue)
                    feeStructure.Amount = dto.Amount.Value;

                if (dto.Description != null)
                    feeStructure.Description = dto.Description;

                if (dto.IsActive.HasValue)
                    feeStructure.IsActive = dto.IsActive.Value;

                feeStructure.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "FeeStructure",
                    feeStructure.FeeStructureId.ToString(),
                    oldData,
                    System.Text.Json.JsonSerializer.Serialize(feeStructure),
                    $"Fee structure updated"
                );

                var response = MapToResponseDto(feeStructure);
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating fee structure");
                return ResponseHelper.Error<FeeStructureResponseDto>("An error occurred");
            }
        }

        public async Task<ApiResponse<string>> DeleteFeeStructureAsync(int feeStructureId, string deletedBy)
        {
            try
            {
                var feeStructure = await _context.FeeStructures.FindAsync(feeStructureId);

                if (feeStructure == null)
                {
                    return ResponseHelper.Error<string>("Fee structure not found");
                }

                var oldData = System.Text.Json.JsonSerializer.Serialize(feeStructure);

                _context.FeeStructures.Remove(feeStructure);
                await _context.SaveChangesAsync();

                // Log
                await _auditService.LogAsync(
                    ActionType.DELETE,
                    "FeeStructure",
                    feeStructureId.ToString(),
                    oldData,
                    null,
                    $"Fee structure deleted"
                );

                return ResponseHelper.Success("Fee structure deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting fee structure");
                return ResponseHelper.Error<string>("An error occurred");
            }
        }

        public async Task<ApiResponse<decimal>> GetTotalCourseFeeAsync(int courseId)
        {
            try
            {
                var totalFee = await _context.FeeStructures
                    .Where(f => f.CourseId == courseId && f.IsActive)
                    .SumAsync(f => f.Amount);

                return ResponseHelper.Success(totalFee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total course fee");
                return ResponseHelper.Error<decimal>("An error occurred");
            }
        }

        private FeeStructureResponseDto MapToResponseDto(FeeStructure feeStructure)
        {
            return new FeeStructureResponseDto
            {
                FeeStructureId = feeStructure.FeeStructureId,
                CourseId = feeStructure.CourseId,
                CourseName = feeStructure.Course?.Name ?? "Unknown",
                FeeType = feeStructure.FeeType,
                Amount = feeStructure.Amount,
                Description = feeStructure.Description,
                IsActive = feeStructure.IsActive,
                CreatedAt = feeStructure.CreatedAt,
                UpdatedAt = feeStructure.UpdatedAt,
                CreatedBy = feeStructure.CreatedBy
            };
        }
    }
}
