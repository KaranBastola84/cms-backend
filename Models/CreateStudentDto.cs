using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class CreateStudentDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required")]
        [Phone(ErrorMessage = "Invalid phone format")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Course is required")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Batch is required")]
        public int BatchId { get; set; }

        public string? Address { get; set; }

        public string? EmergencyContact { get; set; }

        public DateTime? AdmissionDate { get; set; }

        [Required(ErrorMessage = "Total fees is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total fees must be greater than zero")]
        public decimal FeesTotal { get; set; }

        public decimal? FeesPaid { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }
    }
}
