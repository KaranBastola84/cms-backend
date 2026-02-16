using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class CreatePaymentPlanDto
    {
        [Required]
        public int StudentId { get; set; }

        public int? CourseId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Number of installments must be between 1 and 100")]
        public int NumberOfInstallments { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTime? FirstInstallmentDueDate { get; set; }
    }
}
