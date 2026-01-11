using System;

namespace JWTAuthAPI.Models
{
    public class AuditLog
    {
        public int LogId { get; set; }
        public string? UserId { get; set; } // Can be null for anonymous actions
        public string UserEmail { get; set; } = string.Empty; // Store email for reference
        public ActionType ActionType { get; set; }
        public string Module { get; set; } = string.Empty; // e.g., "Inquiry", "Student", "Payment", "User"
        public string EntityId { get; set; } = string.Empty; // ID of the entity being modified
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? PreviousValue { get; set; } // JSON string of old values
        public string? NewValue { get; set; } // JSON string of new values
        public string? AdditionalInfo { get; set; } // Any extra context
        public string IpAddress { get; set; } = string.Empty;
    }

    public enum ActionType
    {
        CREATE,
        UPDATE,
        DELETE,
        LOGIN,
        LOGOUT,
        LOGIN_FAILED,
        PASSWORD_CHANGE,
        ROLE_CHANGE,
        ACCESS_DENIED
    }
}
