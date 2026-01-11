using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
