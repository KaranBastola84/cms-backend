using JWTAuthAPI.Models;

namespace JWTAuthAPI.Services
{
    public interface IReceiptService
    {
        Task<ApiResponse<ReceiptDto>> GenerateReceiptAsync(CreateReceiptDto createDto, string generatedBy);
        Task<ApiResponse<ReceiptDto>> GetReceiptByIdAsync(int receiptId);
        Task<ApiResponse<List<ReceiptDto>>> GetReceiptsByStudentIdAsync(int studentId);
        Task<ApiResponse<ReceiptDto>> GetReceiptByNumberAsync(string receiptNumber);
        Task<ApiResponse<(byte[] FileData, string ContentType, string FileName)>> DownloadReceiptAsync(int receiptId);
        Task<ApiResponse<bool>> DeleteReceiptAsync(int receiptId, string deletedBy);
    }
}
