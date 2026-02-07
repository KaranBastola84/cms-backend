using JWTAuthAPI.Models;

namespace JWTAuthAPI.Services
{
    public interface IStudentService
    {
        Task<ApiResponse<StudentDto>> CreateStudentAsync(CreateStudentDto createDto, string createdBy);
        Task<ApiResponse<StudentDto>> GetStudentByIdAsync(int studentId);
        Task<ApiResponse<List<StudentDto>>> GetAllStudentsAsync();
        Task<ApiResponse<List<StudentDto>>> GetStudentsByStatusAsync(StudentStatus status);
        Task<ApiResponse<StudentDto>> UpdateStudentAsync(int studentId, UpdateStudentDto updateDto, string updatedBy);
        Task<ApiResponse<bool>> DeleteStudentAsync(int studentId, string deletedBy);
        Task<ApiResponse<bool>> ChangeStudentStatusAsync(int studentId, StudentStatus status, string updatedBy);
        Task<ApiResponse<StudentDto>> GetStudentByEmailAsync(string email);
    }
}
