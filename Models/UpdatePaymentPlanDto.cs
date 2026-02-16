using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class UpdatePaymentPlanDto
    {
        public PaymentPlanStatus? Status { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }
    }
}
