using JWTAuthAPI.Data;
using JWTAuthAPI.Helpers;
using JWTAuthAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace JWTAuthAPI.Services
{
    public class BatchService : IBatchService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<BatchService> _logger;

        public BatchService(
            ApplicationDbContext context,
            IAuditService auditService,
            ILogger<BatchService> logger)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<ApiResponse<BatchDto>> CreateBatchAsync(CreateBatchDto createDto, string createdBy)
        {
            try
            {
                // Validate course exists
                var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == createDto.CourseId);
                if (!courseExists)
                {
                    return ResponseHelper.Error<BatchDto>("Course not found");
                }

                var batch = new Batch
                {
                    Name = createDto.Name,
                    CourseId = createDto.CourseId,
                    StartDate = createDto.StartDate,
                    EndDate = createDto.EndDate,
                    TimeSlot = createDto.TimeSlot,
                    MaxStudents = createDto.MaxStudents,
                    IsActive = createDto.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Batches.Add(batch);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    ActionType.CREATE,
                    "Batch",
                    batch.BatchId.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(batch),
                    $"Created batch: {batch.Name}",
                    createdBy
                );

                var batchDto = await MapToDtoAsync(batch);
                return ResponseHelper.Success(batchDto, "Batch created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating batch");
                return ResponseHelper.Error<BatchDto>("Failed to create batch");
            }
        }

        public async Task<ApiResponse<BatchDto>> GetBatchByIdAsync(int batchId)
        {
            try
            {
                var batch = await _context.Batches
                    .Include(b => b.Course)
                    .FirstOrDefaultAsync(b => b.BatchId == batchId);

                if (batch == null)
                {
                    return ResponseHelper.NotFound<BatchDto>("Batch not found");
                }

                var batchDto = await MapToDtoAsync(batch);
                return ResponseHelper.Success(batchDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving batch");
                return ResponseHelper.Error<BatchDto>("Failed to retrieve batch");
            }
        }

        public async Task<ApiResponse<List<BatchDto>>> GetAllBatchesAsync()
        {
            try
            {
                var batches = await _context.Batches
                    .Include(b => b.Course)
                    .OrderByDescending(b => b.StartDate)
                    .ToListAsync();

                var batchDtos = new List<BatchDto>();
                foreach (var batch in batches)
                {
                    batchDtos.Add(await MapToDtoAsync(batch));
                }

                return ResponseHelper.Success(batchDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving batches");
                return ResponseHelper.Error<List<BatchDto>>("Failed to retrieve batches");
            }
        }

        public async Task<ApiResponse<List<BatchDto>>> GetActiveBatchesAsync()
        {
            try
            {
                var batches = await _context.Batches
                    .Include(b => b.Course)
                    .Where(b => b.IsActive)
                    .OrderByDescending(b => b.StartDate)
                    .ToListAsync();

                var batchDtos = new List<BatchDto>();
                foreach (var batch in batches)
                {
                    batchDtos.Add(await MapToDtoAsync(batch));
                }

                return ResponseHelper.Success(batchDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active batches");
                return ResponseHelper.Error<List<BatchDto>>("Failed to retrieve active batches");
            }
        }

        public async Task<ApiResponse<List<BatchDto>>> GetBatchesByCourseAsync(int courseId)
        {
            try
            {
                var batches = await _context.Batches
                    .Include(b => b.Course)
                    .Where(b => b.CourseId == courseId)
                    .OrderByDescending(b => b.StartDate)
                    .ToListAsync();

                var batchDtos = new List<BatchDto>();
                foreach (var batch in batches)
                {
                    batchDtos.Add(await MapToDtoAsync(batch));
                }

                return ResponseHelper.Success(batchDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving batches for course");
                return ResponseHelper.Error<List<BatchDto>>("Failed to retrieve batches for course");
            }
        }

        public async Task<ApiResponse<BatchDto>> UpdateBatchAsync(int batchId, UpdateBatchDto updateDto, string updatedBy)
        {
            try
            {
                var batch = await _context.Batches
                    .Include(b => b.Course)
                    .FirstOrDefaultAsync(b => b.BatchId == batchId);

                if (batch == null)
                {
                    return ResponseHelper.NotFound<BatchDto>("Batch not found");
                }

                if (updateDto.Name != null) batch.Name = updateDto.Name;
                if (updateDto.StartDate.HasValue) batch.StartDate = updateDto.StartDate.Value;
                if (updateDto.EndDate.HasValue) batch.EndDate = updateDto.EndDate;
                if (updateDto.TimeSlot != null) batch.TimeSlot = updateDto.TimeSlot;
                if (updateDto.MaxStudents.HasValue) batch.MaxStudents = updateDto.MaxStudents.Value;
                if (updateDto.IsActive.HasValue) batch.IsActive = updateDto.IsActive.Value;

                batch.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Batch",
                    batch.BatchId.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(batch),
                    $"Updated batch: {batch.Name}",
                    updatedBy
                );

                var batchDto = await MapToDtoAsync(batch);
                return ResponseHelper.Success(batchDto, "Batch updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating batch");
                return ResponseHelper.Error<BatchDto>("Failed to update batch");
            }
        }

        public async Task<ApiResponse<bool>> DeleteBatchAsync(int batchId, string deletedBy)
        {
            try
            {
                var batch = await _context.Batches
                    .Include(b => b.Students)
                    .FirstOrDefaultAsync(b => b.BatchId == batchId);

                if (batch == null)
                {
                    return ResponseHelper.NotFound<bool>("Batch not found");
                }

                if (batch.Students.Any())
                {
                    return ResponseHelper.Error<bool>("Cannot delete batch with associated students");
                }

                _context.Batches.Remove(batch);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    ActionType.DELETE,
                    "Batch",
                    batch.BatchId.ToString(),
                    System.Text.Json.JsonSerializer.Serialize(batch),
                    null,
                    $"Deleted batch: {batch.Name}",
                    deletedBy
                );

                return ResponseHelper.Success(true, "Batch deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting batch");
                return ResponseHelper.Error<bool>("Failed to delete batch");
            }
        }

        private async Task<BatchDto> MapToDtoAsync(Batch batch)
        {
            // Get current student count
            var currentStudents = await _context.Students
                .CountAsync(s => s.BatchId == batch.BatchId);

            return new BatchDto
            {
                BatchId = batch.BatchId,
                Name = batch.Name,
                CourseId = batch.CourseId,
                CourseName = batch.Course?.Name,
                StartDate = batch.StartDate,
                EndDate = batch.EndDate,
                TimeSlot = batch.TimeSlot,
                MaxStudents = batch.MaxStudents,
                CurrentStudents = currentStudents,
                IsActive = batch.IsActive,
                CreatedAt = batch.CreatedAt,
                UpdatedAt = batch.UpdatedAt
            };
        }
    }
}
