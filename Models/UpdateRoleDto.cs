using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Models
{
    public class UpdateRoleDto
    {
        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = string.Empty;
    }
}
