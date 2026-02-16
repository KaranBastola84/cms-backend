using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class UpdateAttendanceDto
    {
        [Required]
        public AttendanceStatus Status { get; set; }

        public TimeSpan? CheckInTime { get; set; }

        public TimeSpan? CheckOutTime { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }
    }
}
