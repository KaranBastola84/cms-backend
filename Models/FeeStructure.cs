using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JWTAuthAPI.Models
{
    public class FeeStructure
    {
        [Key]
        public int FeeStructureId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        [StringLength(100)]
        public string FeeType { get; set; } = string.Empty; // e.g., "Tuition", "Admission", "Lab", "Material"

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation property
        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }
    }
}
