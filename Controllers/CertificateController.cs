using System.Security.Claims;
using JWTAuthAPI.Models;
using JWTAuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JWTAuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CertificateController : ControllerBase
    {
        private readonly ICertificateService _certificateService;

        public CertificateController(ICertificateService certificateService)
        {
            _certificateService = certificateService;
        }

        [HttpPost("recommendations")]
        [Authorize(Roles = Roles.Trainer)]
        public async Task<IActionResult> CreateRecommendation([FromBody] CreateCertificateRecommendationDto dto)
        {
            var trainerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(trainerIdClaim, out var trainerId))
            {
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid trainer token" }
                });
            }

            var result = await _certificateService.RecommendCertificateAsync(dto, trainerId);
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }

        [HttpGet("recommendations")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetPendingRecommendations()
        {
            var result = await _certificateService.GetPendingRecommendationsAsync();
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }

        [HttpGet("{certificateId:int}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Trainer}")]
        public async Task<IActionResult> GetCertificate(int certificateId)
        {
            var result = await _certificateService.GetCertificateByIdAsync(certificateId);
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }

        [HttpGet("{certificateId:int}/eligibility")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetEligibility(int certificateId)
        {
            var result = await _certificateService.GetCertificateEligibilityAsync(certificateId);
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }

        [HttpPost("{certificateId:int}/issue")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> IssueCertificate(int certificateId, [FromBody] IssueCertificateDto dto)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdClaim, out var adminId))
            {
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid admin token" }
                });
            }

            var result = await _certificateService.IssueCertificateAsync(certificateId, dto, adminId);
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }

        [HttpPost("{certificateId:int}/revoke")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> RevokeCertificate(int certificateId, [FromBody] RevokeCertificateDto dto)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdClaim, out var adminId))
            {
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid admin token" }
                });
            }

            var result = await _certificateService.RevokeCertificateAsync(certificateId, dto, adminId);
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }

        [HttpGet("student/{studentId:int}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff},{Roles.Trainer}")]
        public async Task<IActionResult> GetStudentCertificates(int studentId)
        {
            var result = await _certificateService.GetCertificatesByStudentAsync(studentId);
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }

        [HttpGet("student/me")]
        [Authorize(Roles = $"{Roles.Student},{Roles.EnrolledStudent}")]
        public async Task<IActionResult> GetMyCertificates()
        {
            var studentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(studentIdClaim, out var studentId))
            {
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid student token" }
                });
            }

            var result = await _certificateService.GetCertificatesByStudentAsync(studentId);
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }

        [HttpGet("{certificateId:int}/download")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff},{Roles.Trainer},{Roles.Student},{Roles.EnrolledStudent}")]
        public async Task<IActionResult> DownloadCertificate(int certificateId)
        {
            var requesterIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(requesterIdClaim, out var requesterId))
            {
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid token" }
                });
            }

            var requesterRole = User.FindFirst(ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value
                ?? string.Empty;

            var fileResult = await _certificateService.GetCertificateFileAsync(certificateId, requesterId, requesterRole);
            if (!fileResult.IsSuccess || fileResult.Result == null)
            {
                return StatusCode(fileResult.StatusCode, fileResult);
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(fileResult.Result.AbsolutePath);
            return File(bytes, fileResult.Result.ContentType, fileResult.Result.FileName);
        }

        [AllowAnonymous]
        [HttpGet("verify/{certificateNumber}")]
        public async Task<IActionResult> VerifyCertificate(string certificateNumber, [FromQuery] string? token = null)
        {
            var result = await _certificateService.VerifyCertificateAsync(certificateNumber, token);
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }
    }
}
