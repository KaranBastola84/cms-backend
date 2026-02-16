using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JWTAuthAPI.Models
{
    /// <summary>
    /// Tracks which notifications have been read by which users
    /// </summary>
    public class UserNotificationRead
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string NotificationType { get; set; } = string.Empty; // payment, inquiry, attendance, etc.

        [Required]
        public int RelatedId { get; set; } // StudentId, InquiryId, BatchId, etc.

        [Required]
        [StringLength(200)]
        public string NotificationKey { get; set; } = string.Empty; // Unique identifier for the notification

        public DateTime ReadAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}
