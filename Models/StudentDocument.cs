using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JWTAuthAPI.Models
{
    public class StudentDocument
    {
        [Key]
        public int DocumentId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public DocumentType DocumentType { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [StringLength(50)]
        public string ContentType { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation property
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }
    }

    public enum DocumentType
    {
        IdCard,
        Photo,
        Certificate,
        AdmissionForm,
        MarkSheet,
        TransferCertificate,
        Other
    }
}
