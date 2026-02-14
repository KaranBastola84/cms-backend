using System;
using System.Collections.Generic;

namespace JWTAuthAPI.Models
{
    public class Inquiry
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? CourseInterest { get; set; } // What course they're interested in
        public InquiryStatus Status { get; set; } = InquiryStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResponsedAt { get; set; }
        public string? ResponseNotes { get; set; } // Admin/Staff notes

        // Assignment tracking
        public int? AssignedToId { get; set; } // Foreign key to ApplicationUser
        public ApplicationUser? AssignedTo { get; set; } // Navigation property
        public DateTime? AssignedAt { get; set; }

        // Conversion tracking
        public int? ConvertedToStudentId { get; set; } // ID of student created from this inquiry
        public DateTime? ConvertedAt { get; set; }

        // Follow-up notes collection
        public ICollection<FollowUpNote> FollowUpNotes { get; set; } = new List<FollowUpNote>();
    }

    public enum InquiryStatus
    {
        Pending,
        InProgress,
        Contacted,
        Enrolled,
        Rejected,
        Closed
    }
}
