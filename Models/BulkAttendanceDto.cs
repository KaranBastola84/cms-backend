using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class BulkAttendanceDto
    {
        [Required]
        public int BatchId { get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; }

        [Required]
        public List<StudentAttendanceDto> Students { get; set; } = new List<StudentAttendanceDto>();
    }

    public class StudentAttendanceDto
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public AttendanceStatus Status { get; set; }

        public TimeSpan? CheckInTime { get; set; }

        public TimeSpan? CheckOutTime { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }
    }
}
