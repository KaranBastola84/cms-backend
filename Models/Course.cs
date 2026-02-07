using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class Course
    {
        [Key]
        public int CourseId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Code { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public int DurationMonths { get; set; }

        public decimal Fees { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<Batch> Batches { get; set; } = new List<Batch>();
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
