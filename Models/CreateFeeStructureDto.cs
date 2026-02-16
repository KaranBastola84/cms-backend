using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class CreateFeeStructureDto
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        [StringLength(100)]
        public string FeeType { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
