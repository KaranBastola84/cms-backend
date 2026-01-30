using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class UpdateStudentDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone format")]
        public string? Phone { get; set; }

        public int? CourseId { get; set; }

        public int? BatchId { get; set; }

        public StudentStatus? Status { get; set; }

        public string? Address { get; set; }

        public string? EmergencyContact { get; set; }

        public string? DocumentsPath { get; set; }
    }
}
