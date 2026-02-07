using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class UploadDocumentDto
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public DocumentType DocumentType { get; set; }

        [Required]
        public IFormFile File { get; set; } = null!;

        public string? Description { get; set; }
    }

    public class StudentDocumentDto
    {
        public int DocumentId { get; set; }
        public int StudentId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string? Description { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
    }
}
