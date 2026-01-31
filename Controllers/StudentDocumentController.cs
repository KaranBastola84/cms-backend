using JWTAuthAPI.Models;
using JWTAuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JWTAuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentDocumentController : ControllerBase
    {
        private readonly IFileService _fileService;

        public StudentDocumentController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost("upload")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
        [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB limit
        public async Task<IActionResult> UploadDocument([FromForm] int studentId, [FromForm] DocumentType documentType, [FromForm] IFormFile file, [FromForm] string? description)
        {
            if (file == null)
            {
                return BadRequest(new { message = "No file provided" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _fileService.UploadDocumentAsync(studentId, documentType, file, description, userId);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("upload-multiple")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
        [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB total limit
        public async Task<IActionResult> UploadMultipleDocuments([FromForm] int studentId, [FromForm] List<IFormFile> files, [FromForm] List<DocumentType> documentTypes, [FromForm] List<string>? descriptions)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { message = "No files provided" });
            }

            if (files.Count != documentTypes.Count)
            {
                return BadRequest(new { message = "Number of files must match number of document types" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var results = new List<StudentDocumentDto>();
            var errors = new List<string>();

            for (int i = 0; i < files.Count; i++)
            {
                var description = descriptions != null && descriptions.Count > i ? descriptions[i] : null;
                var result = await _fileService.UploadDocumentAsync(studentId, documentTypes[i], files[i], description, userId);

                if (result.IsSuccess && result.Result != null)
                {
                    results.Add(result.Result);
                }
                else
                {
                    errors.AddRange(result.ErrorMessage);
                }
            }

            return Ok(new
            {
                Success = errors.Count == 0,
                UploadedCount = results.Count,
                TotalFiles = files.Count,
                Documents = results,
                Errors = errors
            });
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetStudentDocuments(int studentId)
        {
            var result = await _fileService.GetStudentDocumentsAsync(studentId);
            return Ok(result);
        }

        [HttpGet("{documentId}")]
        public async Task<IActionResult> GetDocumentById(int documentId)
        {
            var result = await _fileService.GetDocumentByIdAsync(documentId);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        [HttpGet("{documentId}/download")]
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            var result = await _fileService.DownloadDocumentAsync(documentId);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            var (fileData, contentType, fileName) = result.Result;
            return File(fileData, contentType, fileName);
        }

        [HttpGet("student/{studentId}/type/{documentType}")]
        public async Task<IActionResult> GetDocumentsByType(int studentId, DocumentType documentType)
        {
            var result = await _fileService.GetDocumentsByTypeAsync(studentId, documentType);
            return Ok(result);
        }

        [HttpDelete("{documentId}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
        public async Task<IActionResult> DeleteDocument(int documentId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _fileService.DeleteDocumentAsync(documentId, userId);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
