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
        private static readonly Dictionary<string, string> PermissionByKey = Permissions.All
            .ToDictionary(p => p.Key, p => p.Key, StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> PermissionByLabel = Permissions.All
            .ToDictionary(p => p.Label, p => p.Key, StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<string>> PermissionsByGroup = Permissions.All
            .GroupBy(p => p.Group, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(p => p.Key).ToList(), StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<string>> LegacyAliasesToCanonical = new(StringComparer.OrdinalIgnoreCase)
        {
            // Staff/Admin navigation aliases used by frontend
            ["view-courses"] = new() { Permissions.CoursesBatches },
            ["course-management"] = new() { Permissions.CoursesBatches },
            ["batch-schedule"] = new() { Permissions.CoursesBatches },
            ["view-inquiries"] = new() { Permissions.Inquiries },
            ["student-registration"] = new() { Permissions.ManageStudents },

            // Trainer navigation aliases used by frontend
            ["view-classes"] = new() { Permissions.CoursesBatches },
            ["view-schedule"] = new() { Permissions.CoursesBatches },
        };

        public PermissionService(ApplicationDbContext context, ILogger<PermissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<PermissionDefinition> GetAllPermissions() => Permissions.All;

        public async Task<List<string>> GetRolePermissionsAsync(string role)
        {
            var normalizedRole = NormalizeRole(role);

            // Admin always has all permissions
            if (normalizedRole == Roles.Admin)
                return Permissions.All.Select(p => p.Key).ToList();

            var storedPermissions = await _context.RolePermissions
                .Where(rp => rp.Role.ToLower() == normalizedRole.ToLower())
                .Select(rp => rp.PermissionKey)
                .ToListAsync();

            var (resolvedPermissions, invalidTokens) = ResolvePermissionTokens(storedPermissions);
            if (invalidTokens.Any())
            {
                _logger.LogWarning("Ignoring unknown permission keys for role {Role}: {Keys}", normalizedRole, string.Join(", ", invalidTokens));
            }

            return resolvedPermissions;
        }

        public async Task<ApiResponse<RolePermissionsDto>> UpdateRolePermissionsAsync(string role, UpdateRolePermissionsDto dto)
        {
            try
            {
                var normalizedRole = NormalizeRole(role);
                if (normalizedRole == Roles.Admin)
                    return ResponseHelper.Error<RolePermissionsDto>("Admin role permissions cannot be modified — Admin always has full access.");

                var (resolvedPermissions, invalidTokens) = ResolvePermissionTokens(dto.Permissions);
                if (invalidTokens.Any())
                    return ResponseHelper.Error<RolePermissionsDto>($"Unknown permission entries: {string.Join(", ", invalidTokens)}");

                // Remove existing and replace with new set
                var existing = await _context.RolePermissions
                    .Where(rp => rp.Role.ToLower() == normalizedRole.ToLower())
                    .ToListAsync();
                _context.RolePermissions.RemoveRange(existing);

                var newPerms = resolvedPermissions.Select(key => new RolePermission
                {
                    Role = normalizedRole,
                    PermissionKey = key
                });
                await _context.RolePermissions.AddRangeAsync(newPerms);
                await _context.SaveChangesAsync();

                return ResponseHelper.Success(new RolePermissionsDto
                {
                    Role = normalizedRole,
                    Permissions = resolvedPermissions
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
                    var (resolvedOverride, invalidOverride) = ResolvePermissionTokens(new[] { o.PermissionKey });
                    if (invalidOverride.Any() || !resolvedOverride.Any())
                    {
                        continue;
                    }

                    var permissionKey = resolvedOverride[0];
                    if (o.IsGranted) effectiveSet.Add(permissionKey);
                    else effectiveSet.Remove(permissionKey);
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

                var (grantResolved, grantInvalid) = ResolvePermissionTokens(dto.Grant);
                var (revokeResolved, revokeInvalid) = ResolvePermissionTokens(dto.Revoke);

                var invalidTokens = grantInvalid
                    .Concat(revokeInvalid)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();

                if (invalidTokens.Any())
                    return ResponseHelper.Error<UserPermissionsDto>($"Unknown permission entries: {string.Join(", ", invalidTokens)}");

                var overlap = grantResolved
                    .Intersect(revokeResolved, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();

                if (overlap.Any())
                    return ResponseHelper.Error<UserPermissionsDto>($"Permission keys cannot be both granted and revoked in the same request: {string.Join(", ", overlap)}");

                // Remove existing overrides for the affected keys, then upsert
                var affectedKeysLower = grantResolved
                    .Concat(revokeResolved)
                    .Select(k => k.ToLower())
                    .ToHashSet();

                var existing = await _context.UserPermissions
                    .Where(up => up.UserId == userId && affectedKeysLower.Contains(up.PermissionKey.ToLower()))
                    .ToListAsync();
                _context.UserPermissions.RemoveRange(existing);

                var newOverrides = grantResolved.Select(key => new UserPermission
                { UserId = userId, PermissionKey = key, IsGranted = true })
                    .Concat(revokeResolved.Select(key => new UserPermission
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
                var normalizedRole = NormalizeRole(role);
                var existingCount = await _context.RolePermissions
                    .CountAsync(rp => rp.Role.ToLower() == normalizedRole.ToLower());
                if (existingCount > 0) continue; // Already seeded

                var (resolvedPermissions, invalidTokens) = ResolvePermissionTokens(permissions);
                if (invalidTokens.Any())
                {
                    _logger.LogWarning("Skipping unknown default permission keys for role {Role}: {Keys}", normalizedRole, string.Join(", ", invalidTokens));
                }

                var entries = resolvedPermissions.Select(key => new RolePermission { Role = normalizedRole, PermissionKey = key });
                await _context.RolePermissions.AddRangeAsync(entries);
            }
            await _context.SaveChangesAsync();
        }

        private static string NormalizeRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return string.Empty;

            var value = role.Trim();
            if (value.Equals(Roles.Admin, StringComparison.OrdinalIgnoreCase)) return Roles.Admin;
            if (value.Equals(Roles.Staff, StringComparison.OrdinalIgnoreCase)) return Roles.Staff;
            if (value.Equals(Roles.Trainer, StringComparison.OrdinalIgnoreCase)) return Roles.Trainer;
            if (value.Equals(Roles.Student, StringComparison.OrdinalIgnoreCase)) return Roles.Student;
            if (value.Equals(Roles.EnrolledStudent, StringComparison.OrdinalIgnoreCase)) return Roles.EnrolledStudent;
            if (value.Equals(Roles.User, StringComparison.OrdinalIgnoreCase)) return Roles.User;
            return value;
        }

        private static (List<string> ResolvedPermissions, List<string> InvalidTokens) ResolvePermissionTokens(IEnumerable<string>? tokens)
        {
            var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var invalid = new List<string>();

            if (tokens == null)
                return (new List<string>(), new List<string>());

            foreach (var token in tokens)
            {
                if (string.IsNullOrWhiteSpace(token)) continue;

                var value = token.Trim();

                if (PermissionByKey.TryGetValue(value, out var key))
                {
                    resolved.Add(key);
                    continue;
                }

                if (PermissionByLabel.TryGetValue(value, out key))
                {
                    resolved.Add(key);
                    continue;
                }

                if (PermissionsByGroup.TryGetValue(value, out var groupedKeys))
                {
                    foreach (var groupedKey in groupedKeys)
                    {
                        resolved.Add(groupedKey);
                    }
                    continue;
                }

                if (LegacyAliasesToCanonical.TryGetValue(value, out var mappedKeys))
                {
                    foreach (var mappedKey in mappedKeys)
                    {
                        resolved.Add(mappedKey);
                    }
                    continue;
                }

                invalid.Add(value);
            }

            return (
                resolved.OrderBy(x => x).ToList(),
                invalid.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList());
        }
    }
}
