using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class CourseDto
    {
        public int CourseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public int DurationMonths { get; set; }
        public decimal Fees { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateCourseDto
    {
        [Required(ErrorMessage = "Course name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Code cannot exceed 50 characters")]
        public string? Code { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Range(1, 120, ErrorMessage = "Duration must be between 1 and 120 months")]
        public int DurationMonths { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Fees must be a positive value")]
        public decimal Fees { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateCourseDto
    {
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string? Name { get; set; }

        [StringLength(50, ErrorMessage = "Code cannot exceed 50 characters")]
        public string? Code { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Range(1, 120, ErrorMessage = "Duration must be between 1 and 120 months")]
        public int? DurationMonths { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Fees must be a positive value")]
        public decimal? Fees { get; set; }

        public bool? IsActive { get; set; }
    }
}
