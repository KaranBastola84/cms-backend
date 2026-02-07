using System;
using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class FollowUpNote
    {
        public int Id { get; set; }
        
        [Required]
        public int InquiryId { get; set; }
        public Inquiry? Inquiry { get; set; } // Navigation property
        
        [Required]
        public int CreatedById { get; set; }
        public ApplicationUser? CreatedBy { get; set; } // Staff/Admin who created the note
        
        [Required]
        [StringLength(1000)]
        public string Note { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
