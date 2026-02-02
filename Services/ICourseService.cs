using JWTAuthAPI.Models;

namespace JWTAuthAPI.Services
{
    public interface ICourseService
    {
        Task<ApiResponse<CourseDto>> CreateCourseAsync(CreateCourseDto createDto, string createdBy);
        Task<ApiResponse<CourseDto>> GetCourseByIdAsync(int courseId);
        Task<ApiResponse<List<CourseDto>>> GetAllCoursesAsync();
        Task<ApiResponse<List<CourseDto>>> GetActiveCoursesAsync();
        Task<ApiResponse<CourseDto>> UpdateCourseAsync(int courseId, UpdateCourseDto updateDto, string updatedBy);
        Task<ApiResponse<bool>> DeleteCourseAsync(int courseId, string deletedBy);
    }
}
