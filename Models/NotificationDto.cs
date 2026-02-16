namespace JWTAuthAPI.Models
{
    public class NotificationResponseDto
    {
        public List<NotificationDto> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }
        public int CriticalCount { get; set; }
        public int WarningCount { get; set; }
    }

    public class NotificationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // payment, attendance, inquiry, batch, admission, payment_received
        public string Severity { get; set; } = string.Empty; // critical, warning, info
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; } = false;
        public string? ActionUrl { get; set; }
        public int? RelatedId { get; set; } // Student/Inquiry/Batch ID
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class MarkNotificationReadDto
    {
        public string NotificationKey { get; set; } = string.Empty;
    }
}
