using JWTAuthAPI.Models;
using JWTAuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JWTAuthAPI.Controllers
{
    [ApiController]
    [Route("api/permissions")]
    [Authorize(Roles = "Admin,Staff")]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionService _permissionService;

        public PermissionsController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        /// <summary>
        /// GET /api/permissions
        /// Returns master list of all permission keys with labels and grouping.
        /// </summary>
        [HttpGet]
        public IActionResult GetAllPermissions()
        {
            var permissions = _permissionService.GetAllPermissions();
            var grouped = permissions
                .GroupBy(p => p.Group)
                .Select(g => new
                {
                    group = g.Key,
                    permissions = g.Select(p => new { p.Key, p.Label })
                });
            return Ok(new { isSuccess = true, statusCode = 200, result = grouped });
        }

        /// <summary>
        /// GET /api/permissions/roles/{role}
        /// Returns all permissions assigned to a role.
        /// </summary>
        [HttpGet("roles/{role}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRolePermissions(string role)
        {
            var permissions = await _permissionService.GetRolePermissionsAsync(role);
            return Ok(new
            {
                isSuccess = true,
                statusCode = 200,
                result = new RolePermissionsDto { Role = role, Permissions = permissions }
            });
        }

        /// <summary>
        /// PUT /api/permissions/roles/{role}
        /// Replaces the permission set for a role.
        /// Body: { "permissions": ["dashboard", "view-students", ...] }
        /// </summary>
        [HttpPut("roles/{role}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRolePermissions(string role, [FromBody] UpdateRolePermissionsDto dto)
        {
            var result = await _permissionService.UpdateRolePermissionsAsync(role, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// GET /api/permissions/users/{id}
        /// Returns effective permissions for a specific user (role defaults + overrides).
        /// </summary>
        [HttpGet("users/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserPermissions(int id)
        {
            var result = await _permissionService.GetUserPermissionsAsync(id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// PUT /api/permissions/users/{id}
        /// Saves per-user permission overrides.
        /// Body: { "grant": ["reports"], "revoke": ["manage-students"] }
        /// </summary>
        [HttpPut("users/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserPermissions(int id, [FromBody] UpdateUserPermissionsDto dto)
        {
            var result = await _permissionService.UpdateUserPermissionsAsync(id, dto);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }
    }
}
