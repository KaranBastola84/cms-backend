using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        public int? CourseId { get; set; }

        public int? BatchId { get; set; }

        [Required]
        public StudentStatus Status { get; set; } = StudentStatus.Enrolled;

        [StringLength(500)]
        public string? DocumentsPath { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? EmergencyContact { get; set; }
    }

    public enum StudentStatus
    {
        Enrolled,
        Active,
        Completed,
        Dropped,
        Suspended
    }
}
