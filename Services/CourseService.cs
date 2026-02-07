using JWTAuthAPI.Data;
using JWTAuthAPI.Helpers;
using JWTAuthAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace JWTAuthAPI.Services
{
    public class CourseService : ICourseService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<CourseService> _logger;

        public CourseService(
            ApplicationDbContext context,
            IAuditService auditService,
            ILogger<CourseService> logger)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<ApiResponse<CourseDto>> CreateCourseAsync(CreateCourseDto createDto, string createdBy)
        {
            try
            {
                var course = new Course
                {
                    Name = createDto.Name,
                    Code = createDto.Code,
                    Description = createDto.Description,
                    DurationMonths = createDto.DurationMonths,
                    Fees = createDto.Fees,
                    IsActive = createDto.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    ActionType.CREATE,
                    "Course",
                    course.CourseId.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(course),
                    $"Created course: {course.Name}",
                    createdBy
                );

                var courseDto = MapToDto(course);
                return ResponseHelper.Success(courseDto, "Course created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course");
                return ResponseHelper.Error<CourseDto>("Failed to create course");
            }
        }

        public async Task<ApiResponse<CourseDto>> GetCourseByIdAsync(int courseId)
        {
            try
            {
                var course = await _context.Courses.FindAsync(courseId);

                if (course == null)
                {
                    return ResponseHelper.NotFound<CourseDto>("Course not found");
                }

                var courseDto = MapToDto(course);
                return ResponseHelper.Success(courseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving course");
                return ResponseHelper.Error<CourseDto>("Failed to retrieve course");
            }
        }

        public async Task<ApiResponse<List<CourseDto>>> GetAllCoursesAsync()
        {
            try
            {
                var courses = await _context.Courses
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                var courseDtos = courses.Select(MapToDto).ToList();
                return ResponseHelper.Success(courseDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving courses");
                return ResponseHelper.Error<List<CourseDto>>("Failed to retrieve courses");
            }
        }

        public async Task<ApiResponse<List<CourseDto>>> GetActiveCoursesAsync()
        {
            try
            {
                var courses = await _context.Courses
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                var courseDtos = courses.Select(MapToDto).ToList();
                return ResponseHelper.Success(courseDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active courses");
                return ResponseHelper.Error<List<CourseDto>>("Failed to retrieve active courses");
            }
        }

        public async Task<ApiResponse<CourseDto>> UpdateCourseAsync(int courseId, UpdateCourseDto updateDto, string updatedBy)
        {
            try
            {
                var course = await _context.Courses.FindAsync(courseId);

                if (course == null)
                {
                    return ResponseHelper.NotFound<CourseDto>("Course not found");
                }

                if (updateDto.Name != null) course.Name = updateDto.Name;
                if (updateDto.Code != null) course.Code = updateDto.Code;
                if (updateDto.Description != null) course.Description = updateDto.Description;
                if (updateDto.DurationMonths.HasValue) course.DurationMonths = updateDto.DurationMonths.Value;
                if (updateDto.Fees.HasValue) course.Fees = updateDto.Fees.Value;
                if (updateDto.IsActive.HasValue) course.IsActive = updateDto.IsActive.Value;

                course.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Course",
                    course.CourseId.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(course),
                    $"Updated course: {course.Name}",
                    updatedBy
                );

                var courseDto = MapToDto(course);
                return ResponseHelper.Success(courseDto, "Course updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course");
                return ResponseHelper.Error<CourseDto>("Failed to update course");
            }
        }

        public async Task<ApiResponse<bool>> DeleteCourseAsync(int courseId, string deletedBy)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.Batches)
                    .Include(c => c.Students)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);

                if (course == null)
                {
                    return ResponseHelper.NotFound<bool>("Course not found");
                }

                if (course.Batches.Any() || course.Students.Any())
                {
                    return ResponseHelper.Error<bool>("Cannot delete course with associated batches or students");
                }

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    ActionType.DELETE,
                    "Course",
                    course.CourseId.ToString(),
                    System.Text.Json.JsonSerializer.Serialize(course),
                    null,
                    $"Deleted course: {course.Name}",
                    deletedBy
                );

                return ResponseHelper.Success(true, "Course deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course");
                return ResponseHelper.Error<bool>("Failed to delete course");
            }
        }

        private static CourseDto MapToDto(Course course)
        {
            return new CourseDto
            {
                CourseId = course.CourseId,
                Name = course.Name,
                Code = course.Code,
                Description = course.Description,
                DurationMonths = course.DurationMonths,
                Fees = course.Fees,
                IsActive = course.IsActive,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt
            };
        }
    }
}
