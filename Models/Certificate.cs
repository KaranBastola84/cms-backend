using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JWTAuthAPI.Models
{
    public class Certificate
    {
        [Key]
        public int CertificateId { get; set; }

        [StringLength(40)]
        public string? CertificateNumber { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        [StringLength(150)]
        public string ModuleName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal TrainerReportedProgressPercent { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal AttendancePercentage { get; set; }

        public bool IsPaymentCleared { get; set; }

        [StringLength(1000)]
        public string? RecommendationNotes { get; set; }

        [Required]
        public int RecommendedByTrainerId { get; set; }

        public DateTime RecommendedAt { get; set; } = DateTime.UtcNow;

        public int? IssuedByAdminId { get; set; }

        public DateTime? IssuedAt { get; set; }

        [Required]
        [StringLength(64)]
        public string VerificationToken { get; set; } = string.Empty;

        [StringLength(500)]
        public string? FilePath { get; set; }

        [Required]
        public CertificateStatus Status { get; set; } = CertificateStatus.Recommended;

        [Required]
        public CertificateDeliveryMode DeliveryMode { get; set; } = CertificateDeliveryMode.Digital;

        [StringLength(1000)]
        public string? AdminNotes { get; set; }

        public DateTime? RevokedAt { get; set; }

        public int? RevokedByAdminId { get; set; }

        [StringLength(500)]
        public string? RevocationReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(StudentId))]
        public virtual Student? Student { get; set; }
    }

    public enum CertificateStatus
    {
        Recommended = 0,
        Issued = 1,
        Revoked = 2
    }

    public enum CertificateDeliveryMode
    {
        Digital = 0,
        OfficePickup = 1
    }

    public class CreateCertificateRecommendationDto
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        [StringLength(150)]
        public string ModuleName { get; set; } = string.Empty;

        [Range(0, 100)]
        public decimal ProgressPercent { get; set; }

        [StringLength(1000)]
        public string? RecommendationNotes { get; set; }
    }

    public class IssueCertificateDto
    {
        public CertificateDeliveryMode DeliveryMode { get; set; } = CertificateDeliveryMode.Digital;

        [StringLength(1000)]
        public string? AdminNotes { get; set; }
    }

    public class RevokeCertificateDto
    {
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class CertificateDto
    {
        public int CertificateId { get; set; }
        public string? CertificateNumber { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public int? CourseId { get; set; }
        public string? CourseName { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public decimal TrainerReportedProgressPercent { get; set; }
        public decimal AttendancePercentage { get; set; }
        public bool IsPaymentCleared { get; set; }
        public CertificateStatus Status { get; set; }
        public CertificateDeliveryMode DeliveryMode { get; set; }
        public string? RecommendationNotes { get; set; }
        public string? AdminNotes { get; set; }
        public int RecommendedByTrainerId { get; set; }
        public string? RecommendedByTrainerName { get; set; }
        public DateTime RecommendedAt { get; set; }
        public int? IssuedByAdminId { get; set; }
        public string? IssuedByAdminName { get; set; }
        public DateTime? IssuedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevocationReason { get; set; }
        public string? DownloadUrl { get; set; }
        public string? VerificationUrl { get; set; }
    }

    public class CertificateEligibilityDto
    {
        public int StudentId { get; set; }
        public decimal AttendancePercentage { get; set; }
        public bool IsAttendanceEligible { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal FeesTotal { get; set; }
        public bool IsPaymentCleared { get; set; }
        public bool IsEligible { get; set; }
        public List<string> Reasons { get; set; } = new();
    }

    public class CertificateFileDto
    {
        public string AbsolutePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/pdf";
    }

    public class CertificateVerificationDto
    {
        public bool IsValid { get; set; }
        public string CertificateNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string? CourseName { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public DateTime? IssuedAt { get; set; }
        public CertificateStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
