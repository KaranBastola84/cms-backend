using JWTAuthAPI.Data;
using JWTAuthAPI.Helpers;
using JWTAuthAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace JWTAuthAPI.Services
{
    public class FileService : IFileService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<FileService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly string _uploadsFolder;

        // Allowed file extensions and max size
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly string[] _allowedDocumentExtensions = { ".pdf", ".doc", ".docx" };
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5 MB

        public FileService(
            ApplicationDbContext context,
            IAuditService auditService,
            ILogger<FileService> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
            _environment = environment;
            _uploadsFolder = Path.Combine(_environment.ContentRootPath, "Uploads", "StudentDocuments");

            // Ensure uploads directory exists
            if (!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
            }
        }

        public async Task<ApiResponse<StudentDocumentDto>> UploadDocumentAsync(
            int studentId,
            DocumentType documentType,
            IFormFile file,
            string? description,
            string uploadedBy)
        {
            try
            {
                // Check if student exists
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                {
                    return ResponseHelper.Error<StudentDocumentDto>("Student not found");
                }

                // Validate file
                var validationResult = ValidateFile(file, documentType);
                if (!validationResult.IsValid)
                {
                    return ResponseHelper.Error<StudentDocumentDto>(validationResult.ErrorMessage!);
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{studentId}_{documentType}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(_uploadsFolder, uniqueFileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create document record
                var document = new StudentDocument
                {
                    StudentId = studentId,
                    DocumentType = documentType,
                    FileName = file.FileName,
                    FilePath = filePath,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    Description = description,
                    UploadedAt = DateTime.UtcNow
                };

                _context.StudentDocuments.Add(document);
                await _context.SaveChangesAsync();

                // Log the action
                await _auditService.LogAsync(
                    ActionType.CREATE,
                    "StudentDocument",
                    document.DocumentId.ToString(),
                    null,
                    $"Uploaded {documentType} document for student {student.Name}",
                    $"File: {file.FileName}, Size: {file.Length} bytes",
                    uploadedBy
                );

                var documentDto = MapToDto(document);
                return ResponseHelper.Success(documentDto, "Document uploaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return ResponseHelper.Error<StudentDocumentDto>("An error occurred while uploading the document");
            }
        }

        public async Task<ApiResponse<List<StudentDocumentDto>>> GetStudentDocumentsAsync(int studentId)
        {
            try
            {
                var documents = await _context.StudentDocuments
                    .Where(d => d.StudentId == studentId)
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();

                var documentDtos = documents.Select(MapToDto).ToList();
                return ResponseHelper.Success(documentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student documents");
                return ResponseHelper.Error<List<StudentDocumentDto>>("An error occurred while retrieving documents");
            }
        }

        public async Task<ApiResponse<StudentDocumentDto>> GetDocumentByIdAsync(int documentId)
        {
            try
            {
                var document = await _context.StudentDocuments.FindAsync(documentId);

                if (document == null)
                {
                    return ResponseHelper.Error<StudentDocumentDto>("Document not found");
                }

                var documentDto = MapToDto(document);
                return ResponseHelper.Success(documentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document");
                return ResponseHelper.Error<StudentDocumentDto>("An error occurred while retrieving the document");
            }
        }

        public async Task<ApiResponse<(byte[] FileData, string ContentType, string FileName)>> DownloadDocumentAsync(int documentId)
        {
            try
            {
                var document = await _context.StudentDocuments.FindAsync(documentId);

                if (document == null)
                {
                    return ResponseHelper.Error<(byte[], string, string)>("Document not found");
                }

                if (!File.Exists(document.FilePath))
                {
                    return ResponseHelper.Error<(byte[], string, string)>("File not found on server");
                }

                var fileData = await File.ReadAllBytesAsync(document.FilePath);
                return ResponseHelper.Success((fileData, document.ContentType, document.FileName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document");
                return ResponseHelper.Error<(byte[], string, string)>("An error occurred while downloading the document");
            }
        }

        public async Task<ApiResponse<bool>> DeleteDocumentAsync(int documentId, string deletedBy)
        {
            try
            {
                var document = await _context.StudentDocuments.FindAsync(documentId);

                if (document == null)
                {
                    return ResponseHelper.Error<bool>("Document not found");
                }

                // Delete physical file
                if (File.Exists(document.FilePath))
                {
                    File.Delete(document.FilePath);
                }

                // Delete database record
                _context.StudentDocuments.Remove(document);
                await _context.SaveChangesAsync();

                // Log the action
                await _auditService.LogAsync(
                    ActionType.DELETE,
                    "StudentDocument",
                    document.DocumentId.ToString(),
                    $"{document.DocumentType} - {document.FileName}",
                    null,
                    $"Deleted document for student ID {document.StudentId}",
                    deletedBy
                );

                return ResponseHelper.Success(true, "Document deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document");
                return ResponseHelper.Error<bool>("An error occurred while deleting the document");
            }
        }

        public async Task<ApiResponse<List<StudentDocumentDto>>> GetDocumentsByTypeAsync(int studentId, DocumentType documentType)
        {
            try
            {
                var documents = await _context.StudentDocuments
                    .Where(d => d.StudentId == studentId && d.DocumentType == documentType)
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();

                var documentDtos = documents.Select(MapToDto).ToList();
                return ResponseHelper.Success(documentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents by type");
                return ResponseHelper.Error<List<StudentDocumentDto>>("An error occurred while retrieving documents");
            }
        }

        private (bool IsValid, string? ErrorMessage) ValidateFile(IFormFile file, DocumentType documentType)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "No file provided");
            }

            if (file.Length > _maxFileSize)
            {
                return (false, $"File size exceeds maximum allowed size of {_maxFileSize / 1024 / 1024} MB");
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // Check file extension based on document type
            if (documentType == DocumentType.Photo)
            {
                if (!_allowedImageExtensions.Contains(fileExtension))
                {
                    return (false, $"Invalid file type. Allowed types for photos: {string.Join(", ", _allowedImageExtensions)}");
                }
            }
            else
            {
                var allAllowedExtensions = _allowedImageExtensions.Concat(_allowedDocumentExtensions);
                if (!allAllowedExtensions.Contains(fileExtension))
                {
                    return (false, $"Invalid file type. Allowed types: {string.Join(", ", allAllowedExtensions)}");
                }
            }

            return (true, null);
        }

        private StudentDocumentDto MapToDto(StudentDocument document)
        {
            return new StudentDocumentDto
            {
                DocumentId = document.DocumentId,
                StudentId = document.StudentId,
                DocumentType = document.DocumentType.ToString(),
                FileName = document.FileName,
                FileSize = document.FileSize,
                ContentType = document.ContentType,
                UploadedAt = document.UploadedAt,
                Description = document.Description,
                DownloadUrl = $"/api/StudentDocument/{document.DocumentId}/download"
            };
        }
    }
}
