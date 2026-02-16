using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class UpdateFeeStructureDto
    {
        [StringLength(100)]
        public string? FeeType { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? Amount { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }
    }
}
