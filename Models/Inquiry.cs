using System;

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
        public DateTime? ResponsedAt { get; set; }
        public string? ResponseNotes { get; set; } // Admin/Staff notes
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
