using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class BatchDto
    {
        public int BatchId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string? CourseName { get; set; }
        public int? TrainerId { get; set; }
        public string? TrainerName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? TimeSlot { get; set; }
        public int MaxStudents { get; set; }
        public int CurrentStudents { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateBatchDto
    {
        [Required(ErrorMessage = "Batch name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Course ID is required")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [StringLength(50, ErrorMessage = "Time slot cannot exceed 50 characters")]
        public string? TimeSlot { get; set; }

        public int? TrainerId { get; set; }

        [Range(1, 500, ErrorMessage = "Max students must be between 1 and 500")]
        public int MaxStudents { get; set; } = 30;

        public bool IsActive { get; set; } = true;
    }

    public class UpdateBatchDto
    {
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? Name { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [StringLength(50, ErrorMessage = "Time slot cannot exceed 50 characters")]
        public string? TimeSlot { get; set; }

        public int? TrainerId { get; set; }

        [Range(1, 500, ErrorMessage = "Max students must be between 1 and 500")]
        public int? MaxStudents { get; set; }

        public bool? IsActive { get; set; }
    }
}
