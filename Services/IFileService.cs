using JWTAuthAPI.Models;

namespace JWTAuthAPI.Services
{
    public interface IFileService
    {
        Task<ApiResponse<StudentDocumentDto>> UploadDocumentAsync(int studentId, DocumentType documentType, IFormFile file, string? description, string uploadedBy);
        Task<ApiResponse<List<StudentDocumentDto>>> GetStudentDocumentsAsync(int studentId);
        Task<ApiResponse<StudentDocumentDto>> GetDocumentByIdAsync(int documentId);
        Task<ApiResponse<(byte[] FileData, string ContentType, string FileName)>> DownloadDocumentAsync(int documentId);
        Task<ApiResponse<bool>> DeleteDocumentAsync(int documentId, string deletedBy);
        Task<ApiResponse<List<StudentDocumentDto>>> GetDocumentsByTypeAsync(int studentId, DocumentType documentType);
    }
}
