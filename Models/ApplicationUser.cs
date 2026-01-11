using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JWTAuthAPI.Models
{
    public class ApplicationUser
    {
        public int Id { get; set; } // Primary key
        public string Username  { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // hashed password
        public string Role { get; set; } = "User"; // Default role is "User"
        public bool IsActive { get; set; } = true; // Default is active
        public string? RefreshToken { get; set; } // Nullable refresh token
        public DateTime? RefreshTokenExpiryTime { get; set; } // Nullable expiry time
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Timestamp of creation
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Timestamp of last update
    }
}