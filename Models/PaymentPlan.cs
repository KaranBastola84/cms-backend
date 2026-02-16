using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JWTAuthAPI.Models
{
    public class PaymentPlan
    {
        [Key]
        public int PaymentPlanId { get; set; }

        [Required]
        public int StudentId { get; set; }

        public int? CourseId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PaidAmount { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        public decimal BalanceAmount { get; set; }

        [Required]
        public int NumberOfInstallments { get; set; }

        [Required]
        public PaymentPlanStatus Status { get; set; } = PaymentPlanStatus.Active;

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        public virtual ICollection<Installment> Installments { get; set; } = new List<Installment>();
    }
}
