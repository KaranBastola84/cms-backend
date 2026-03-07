using JWTAuthAPI.Data;
using JWTAuthAPI.Helpers;
using JWTAuthAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace JWTAuthAPI.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(ApplicationDbContext context, ILogger<PermissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<PermissionDefinition> GetAllPermissions() => Permissions.All;

        public async Task<List<string>> GetRolePermissionsAsync(string role)
        {
            // Admin always has all permissions
            if (role == Roles.Admin)
                return Permissions.All.Select(p => p.Key).ToList();

            return await _context.RolePermissions
                .Where(rp => rp.Role == role)
                .Select(rp => rp.PermissionKey)
                .ToListAsync();
        }

        public async Task<ApiResponse<RolePermissionsDto>> UpdateRolePermissionsAsync(string role, UpdateRolePermissionsDto dto)
        {
            try
            {
                if (role == Roles.Admin)
                    return ResponseHelper.Error<RolePermissionsDto>("Admin role permissions cannot be modified — Admin always has full access.");

                var validKeys = Permissions.All.Select(p => p.Key).ToHashSet();
                var invalid = dto.Permissions.Where(p => !validKeys.Contains(p)).ToList();
                if (invalid.Any())
                    return ResponseHelper.Error<RolePermissionsDto>($"Unknown permission keys: {string.Join(", ", invalid)}");

                // Remove existing and replace with new set
                var existing = await _context.RolePermissions.Where(rp => rp.Role == role).ToListAsync();
                _context.RolePermissions.RemoveRange(existing);

                var newPerms = dto.Permissions.Distinct().Select(key => new RolePermission
                {
                    Role = role,
                    PermissionKey = key
                });
                await _context.RolePermissions.AddRangeAsync(newPerms);
                await _context.SaveChangesAsync();

                return ResponseHelper.Success(new RolePermissionsDto
                {
                    Role = role,
                    Permissions = dto.Permissions.Distinct().ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permissions for role {Role}", role);
                return ResponseHelper.Error<RolePermissionsDto>("An error occurred while updating role permissions");
            }
        }

        public async Task<ApiResponse<UserPermissionsDto>> GetUserPermissionsAsync(int userId)
        {
            try
            {
                var user = await _context.ApplicationUsers.FindAsync(userId);
                if (user == null)
                    return ResponseHelper.Error<UserPermissionsDto>("User not found", 404);

                var rolePerms = await GetRolePermissionsAsync(user.Role);
                var overrides = await _context.UserPermissions.Where(up => up.UserId == userId).ToListAsync();

                var effectiveSet = new HashSet<string>(rolePerms);
                foreach (var o in overrides)
                {
                    if (o.IsGranted) effectiveSet.Add(o.PermissionKey);
                    else effectiveSet.Remove(o.PermissionKey);
                }

                return ResponseHelper.Success(new UserPermissionsDto
                {
                    UserId = userId,
                    Username = user.Username,
                    Role = user.Role,
                    RolePermissions = rolePerms,
                    GrantedOverrides = overrides.Where(o => o.IsGranted).Select(o => o.PermissionKey).ToList(),
                    RevokedOverrides = overrides.Where(o => !o.IsGranted).Select(o => o.PermissionKey).ToList(),
                    EffectivePermissions = effectiveSet.OrderBy(p => p).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
                return ResponseHelper.Error<UserPermissionsDto>("An error occurred while retrieving user permissions");
            }
        }

        public async Task<ApiResponse<UserPermissionsDto>> UpdateUserPermissionsAsync(int userId, UpdateUserPermissionsDto dto)
        {
            try
            {
                var user = await _context.ApplicationUsers.FindAsync(userId);
                if (user == null)
                    return ResponseHelper.Error<UserPermissionsDto>("User not found", 404);

                var validKeys = Permissions.All.Select(p => p.Key).ToHashSet();
                var allRequested = dto.Grant.Concat(dto.Revoke).ToList();
                var invalid = allRequested.Where(p => !validKeys.Contains(p)).ToList();
                if (invalid.Any())
                    return ResponseHelper.Error<UserPermissionsDto>($"Unknown permission keys: {string.Join(", ", invalid)}");

                // Remove existing overrides for the affected keys, then upsert
                var affectedKeys = allRequested.ToHashSet();
                var existing = await _context.UserPermissions
                    .Where(up => up.UserId == userId && affectedKeys.Contains(up.PermissionKey))
                    .ToListAsync();
                _context.UserPermissions.RemoveRange(existing);

                var newOverrides = dto.Grant.Distinct().Select(key => new UserPermission
                { UserId = userId, PermissionKey = key, IsGranted = true })
                    .Concat(dto.Revoke.Distinct().Select(key => new UserPermission
                    { UserId = userId, PermissionKey = key, IsGranted = false }));

                await _context.UserPermissions.AddRangeAsync(newOverrides);
                await _context.SaveChangesAsync();

                return await GetUserPermissionsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permissions for user {UserId}", userId);
                return ResponseHelper.Error<UserPermissionsDto>("An error occurred while updating user permissions");
            }
        }

        public async Task SeedDefaultPermissionsAsync()
        {
            foreach (var (role, permissions) in Permissions.RoleDefaults)
            {
                var existingCount = await _context.RolePermissions.CountAsync(rp => rp.Role == role);
                if (existingCount > 0) continue; // Already seeded

                var entries = permissions.Select(key => new RolePermission { Role = role, PermissionKey = key });
                await _context.RolePermissions.AddRangeAsync(entries);
            }
            await _context.SaveChangesAsync();
        }
    }
}
