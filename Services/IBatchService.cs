using JWTAuthAPI.Models;

namespace JWTAuthAPI.Services
{
    public interface IBatchService
    {
        Task<ApiResponse<BatchDto>> CreateBatchAsync(CreateBatchDto createDto, string createdBy);
        Task<ApiResponse<BatchDto>> GetBatchByIdAsync(int batchId);
        Task<ApiResponse<List<BatchDto>>> GetAllBatchesAsync();
        Task<ApiResponse<List<BatchDto>>> GetActiveBatchesAsync();
        Task<ApiResponse<List<BatchDto>>> GetBatchesByCourseAsync(int courseId);
        Task<ApiResponse<BatchDto>> UpdateBatchAsync(int batchId, UpdateBatchDto updateDto, string updatedBy);
        Task<ApiResponse<bool>> DeleteBatchAsync(int batchId, string deletedBy);
    }
}
