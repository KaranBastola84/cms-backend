using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class Batch
    {
        [Key]
        public int BatchId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int CourseId { get; set; }

        public int? TrainerId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [StringLength(50)]
        public string? TimeSlot { get; set; }

        public int MaxStudents { get; set; } = 30;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Course Course { get; set; } = null!;
        public ApplicationUser? Trainer { get; set; }
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
