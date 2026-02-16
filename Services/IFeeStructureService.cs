using JWTAuthAPI.Models;

namespace JWTAuthAPI.Services
{
    public interface IFeeStructureService
    {
        Task<ApiResponse<FeeStructureResponseDto>> CreateFeeStructureAsync(CreateFeeStructureDto dto, string createdBy);
        Task<ApiResponse<FeeStructureResponseDto>> GetFeeStructureByIdAsync(int feeStructureId);
        Task<ApiResponse<List<FeeStructureResponseDto>>> GetFeeStructuresByCourseIdAsync(int courseId);
        Task<ApiResponse<List<FeeStructureResponseDto>>> GetAllFeeStructuresAsync();
        Task<ApiResponse<FeeStructureResponseDto>> UpdateFeeStructureAsync(int feeStructureId, UpdateFeeStructureDto dto, string updatedBy);
        Task<ApiResponse<string>> DeleteFeeStructureAsync(int feeStructureId, string deletedBy);
        Task<ApiResponse<decimal>> GetTotalCourseFeeAsync(int courseId);
    }
}
