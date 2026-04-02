using JWTAuthAPI.Models;

namespace JWTAuthAPI.Services
{
    public interface ICertificateService
    {
        Task<ApiResponse<CertificateDto>> RecommendCertificateAsync(CreateCertificateRecommendationDto dto, int trainerId);
        Task<ApiResponse<List<CertificateDto>>> GetPendingRecommendationsAsync();
        Task<ApiResponse<CertificateDto>> GetCertificateByIdAsync(int certificateId);
        Task<ApiResponse<CertificateEligibilityDto>> GetCertificateEligibilityAsync(int certificateId);
        Task<ApiResponse<CertificateDto>> IssueCertificateAsync(int certificateId, IssueCertificateDto dto, int adminId);
        Task<ApiResponse<CertificateDto>> RevokeCertificateAsync(int certificateId, RevokeCertificateDto dto, int adminId);
        Task<ApiResponse<List<CertificateDto>>> GetCertificatesByStudentAsync(int studentId);
        Task<ApiResponse<CertificateFileDto>> GetCertificateFileAsync(int certificateId, int requesterId, string requesterRole);
        Task<ApiResponse<CertificateVerificationDto>> VerifyCertificateAsync(string certificateNumber, string? token);
    }
}
