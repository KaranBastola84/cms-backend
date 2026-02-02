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

        public int? CourseId { get; set; }

        public int? BatchId { get; set; }

        public string? Address { get; set; }

        public string? EmergencyContact { get; set; }

        public DateTime? AdmissionDate { get; set; }

        public decimal? FeesTotal { get; set; }

        public decimal? FeesPaid { get; set; }
    }
}
