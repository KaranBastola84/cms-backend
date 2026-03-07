using JWTAuthAPI.Models;

namespace JWTAuthAPI.Services
{
    public interface IPermissionService
    {
        /// <summary>Returns all available permission definitions (key, label, group).</summary>
        List<PermissionDefinition> GetAllPermissions();

        /// <summary>Returns the resolved permission keys for a role.</summary>
        Task<List<string>> GetRolePermissionsAsync(string role);

        /// <summary>Replaces all permissions for a role with the provided list.</summary>
        Task<ApiResponse<RolePermissionsDto>> UpdateRolePermissionsAsync(string role, UpdateRolePermissionsDto dto);

        /// <summary>Returns the fully resolved (effective) permissions for a user = role defaults + overrides.</summary>
        Task<ApiResponse<UserPermissionsDto>> GetUserPermissionsAsync(int userId);

        /// <summary>Saves per-user grant/revoke overrides on top of role defaults.</summary>
        Task<ApiResponse<UserPermissionsDto>> UpdateUserPermissionsAsync(int userId, UpdateUserPermissionsDto dto);

        /// <summary>Seeds default role permissions if they don't yet exist in DB.</summary>
        Task SeedDefaultPermissionsAsync();
    }
}
