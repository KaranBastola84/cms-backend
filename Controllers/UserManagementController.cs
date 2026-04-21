using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JWTAuthAPI.Data;
using JWTAuthAPI.Models;

namespace JWTAuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires authentication
    public class UserManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly Services.IAuditService _auditService;
        private readonly IPasswordHasher<Student> _studentPasswordHasher;

        public UserManagementController(
            ApplicationDbContext context,
            Services.IAuditService auditService,
            IPasswordHasher<Student> studentPasswordHasher)
        {
            _context = context;
            _auditService = auditService;
            _studentPasswordHasher = studentPasswordHasher;
        }

        // Admin only
        [HttpGet("users")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff},{Roles.Trainer}")]
        public async Task<IActionResult> GetAllUsers()
        {
            var isAdmin = User.IsInRole(Roles.Admin);

            var query = _context.ApplicationUsers.AsQueryable();
            if (!isAdmin)
            {
                // Non-admins only need assignable active users for operational pages like inquiries.
                query = query.Where(u => u.IsActive &&
                    (u.Role == Roles.Admin || u.Role == Roles.Staff || u.Role == Roles.Trainer));
            }

            var users = await query
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.IsActive,
                    u.FirstName,
                    u.LastName,
                    u.PhoneNumber,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .ToListAsync();

            return Ok(new ApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Result = users
            });
        }

        // Admin only
        [HttpGet("users/{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.ApplicationUsers
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.IsActive,
                    u.FirstName,
                    u.LastName,
                    u.PhoneNumber,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new ApiResponse<string>
                {
                    StatusCode = 404,
                    IsSuccess = false,
                    ErrorMessage = { "User not found" }
                });

            return Ok(new ApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Result = user
            });
        }

        // Admin only
        [HttpDelete("users/{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.ApplicationUsers.FindAsync(id);

            if (user == null)
                return NotFound(new ApiResponse<string>
                {
                    StatusCode = 404,
                    IsSuccess = false,
                    ErrorMessage = { "User not found" }
                });

            // Prevent admin from deleting themselves
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id.ToString())
                return BadRequest(new ApiResponse<string>
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    ErrorMessage = { "You cannot delete your own account" }
                });

            // Store user data before deletion
            var userData = System.Text.Json.JsonSerializer.Serialize(new { user.Username, user.Email, user.Role });

            _context.ApplicationUsers.Remove(user);
            await _context.SaveChangesAsync();

            // Log user deletion
            await _auditService.LogAsync(
                ActionType.DELETE,
                "User",
                user.Id.ToString(),
                userData,
                null,
                $"User {user.Username} deleted by admin"
            );

            return Ok(new ApiResponse<string>
            {
                StatusCode = 200,
                IsSuccess = true,
                Result = "User deleted successfully"
            });
        }

        // Admin only
        [HttpPut("users/{id}/deactivate")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var user = await _context.ApplicationUsers.FindAsync(id);

            if (user == null)
                return NotFound(new ApiResponse<string>
                {
                    StatusCode = 404,
                    IsSuccess = false,
                    ErrorMessage = { "User not found" }
                });

            // Prevent admin from deactivating themselves
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id.ToString())
                return BadRequest(new ApiResponse<string>
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    ErrorMessage = { "You cannot deactivate your own account" }
                });

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Log user deactivation
            await _auditService.LogAsync(
                ActionType.UPDATE,
                "User",
                user.Id.ToString(),
                System.Text.Json.JsonSerializer.Serialize(new { IsActive = true }),
                System.Text.Json.JsonSerializer.Serialize(new { IsActive = false }),
                $"User {user.Username} deactivated"
            );

            return Ok(new ApiResponse<string>
            {
                StatusCode = 200,
                IsSuccess = true,
                Result = "User deactivated successfully"
            });
        }

        // Admin only
        [HttpPut("users/{id}/activate")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> ActivateUser(int id)
        {
            var user = await _context.ApplicationUsers.FindAsync(id);

            if (user == null)
                return NotFound(new ApiResponse<string>
                {
                    StatusCode = 404,
                    IsSuccess = false,
                    ErrorMessage = { "User not found" }
                });

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Log user activation
            await _auditService.LogAsync(
                ActionType.UPDATE,
                "User",
                user.Id.ToString(),
                System.Text.Json.JsonSerializer.Serialize(new { IsActive = false }),
                System.Text.Json.JsonSerializer.Serialize(new { IsActive = true }),
                $"User {user.Username} activated"
            );

            return Ok(new ApiResponse<string>
            {
                StatusCode = 200,
                IsSuccess = true,
                Result = "User activated successfully"
            });
        }

        // Admin only
        [HttpPut("users/{id}/role")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleDto roleDto)
        {
            var user = await _context.ApplicationUsers.FindAsync(id);

            if (user == null)
                return NotFound(new ApiResponse<string>
                {
                    StatusCode = 404,
                    IsSuccess = false,
                    ErrorMessage = { "User not found" }
                });

            // Validate role
            if (roleDto.Role != Roles.Admin &&
                roleDto.Role != Roles.Staff &&
                roleDto.Role != Roles.Trainer &&
                roleDto.Role != Roles.Student)
                return BadRequest(new ApiResponse<string>
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid role. Allowed values: Admin, Staff, Trainer, Student" }
                });

            // Prevent admin from changing their own role
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id.ToString())
                return BadRequest(new ApiResponse<string>
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    ErrorMessage = { "You cannot change your own role" }
                });

            var oldRole = user.Role;
            user.Role = roleDto.Role;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Log role change
            await _auditService.LogAsync(
                ActionType.ROLE_CHANGE,
                "User",
                user.Id.ToString(),
                System.Text.Json.JsonSerializer.Serialize(new { Role = oldRole }),
                System.Text.Json.JsonSerializer.Serialize(new { Role = user.Role }),
                $"User {user.Username} role changed from {oldRole} to {user.Role}"
            );

            return Ok(new ApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Result = new { message = "User role updated successfully", newRole = user.Role }
            });
        }

        // Both Admin and User
        [HttpGet("profile")]
        [Authorize] // Any authenticated user
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value
                ?? string.Empty;
            var isStudentRole = role.Equals(Roles.Student, StringComparison.OrdinalIgnoreCase)
                || role.Equals(Roles.EnrolledStudent, StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid token" }
                });

            if (isStudentRole)
            {
                var student = await _context.Students
                    .Where(s => s.StudentId == id)
                    .Select(s => new
                    {
                        s.StudentId,
                        s.Name,
                        s.Email,
                        s.Phone,
                        s.Status,
                        s.CreatedAt,
                        s.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (student == null)
                    return NotFound(new ApiResponse<string>
                    {
                        StatusCode = 404,
                        IsSuccess = false,
                        ErrorMessage = { "Student not found" }
                    });

                var parts = (student.Name ?? string.Empty)
                    .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var firstName = parts.Length > 0 ? parts[0] : string.Empty;
                var lastName = parts.Length > 1 ? parts[1] : string.Empty;

                return Ok(new ApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Result = new
                    {
                        Id = student.StudentId,
                        Username = student.Email,
                        student.Email,
                        Role = Roles.Student,
                        IsActive = student.Status != StudentStatus.Suspended && student.Status != StudentStatus.Dropped,
                        FirstName = firstName,
                        LastName = lastName,
                        PhoneNumber = student.Phone,
                        student.CreatedAt,
                        student.UpdatedAt
                    }
                });
            }

            var user = await _context.ApplicationUsers
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.IsActive,
                    u.FirstName,
                    u.LastName,
                    u.PhoneNumber,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new ApiResponse<string>
                {
                    StatusCode = 404,
                    IsSuccess = false,
                    ErrorMessage = { "User not found" }
                });

            return Ok(new ApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Result = user
            });
        }

        // Any authenticated user - Update their own profile
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto profileDto)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value
                ?? string.Empty;
            var isStudentRole = role.Equals(Roles.Student, StringComparison.OrdinalIgnoreCase)
                || role.Equals(Roles.EnrolledStudent, StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid token" }
                });

            if (isStudentRole)
            {
                var student = await _context.Students.FindAsync(id);

                if (student == null)
                    return NotFound(new ApiResponse<string>
                    {
                        StatusCode = 404,
                        IsSuccess = false,
                        ErrorMessage = { "Student not found" }
                    });

                student.Name = $"{profileDto.FirstName} {profileDto.LastName}".Trim();
                if (!string.IsNullOrWhiteSpace(profileDto.PhoneNumber))
                {
                    student.Phone = profileDto.PhoneNumber;
                }
                student.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Student",
                    student.StudentId.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(new { student.Name, student.Phone }),
                    $"Student {student.Email} updated profile",
                    student.StudentId.ToString(),
                    student.Email
                );

                var parts = student.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var firstName = parts.Length > 0 ? parts[0] : string.Empty;
                var lastName = parts.Length > 1 ? parts[1] : string.Empty;

                return Ok(new ApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Result = new
                    {
                        Id = student.StudentId,
                        Username = student.Email,
                        student.Email,
                        FirstName = firstName,
                        LastName = lastName,
                        PhoneNumber = student.Phone,
                        student.UpdatedAt
                    }
                });
            }

            var user = await _context.ApplicationUsers.FindAsync(id);

            if (user == null)
                return NotFound(new ApiResponse<string>
                {
                    StatusCode = 404,
                    IsSuccess = false,
                    ErrorMessage = { "User not found" }
                });

            // Update profile fields
            user.FirstName = profileDto.FirstName;
            user.LastName = profileDto.LastName;
            user.PhoneNumber = profileDto.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log profile update
            await _auditService.LogAsync(
                ActionType.UPDATE,
                "User",
                user.Id.ToString(),
                null,
                System.Text.Json.JsonSerializer.Serialize(new { user.FirstName, user.LastName, user.PhoneNumber }),
                $"User {user.Username} updated their profile",
                user.Id.ToString(),
                user.Email
            );

            return Ok(new ApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Result = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.PhoneNumber,
                    user.UpdatedAt
                }
            });
        }

        // Any authenticated user
        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            // Get current user ID from token
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value
                ?? string.Empty;
            var isStudentRole = role.Equals(Roles.Student, StringComparison.OrdinalIgnoreCase)
                || role.Equals(Roles.EnrolledStudent, StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid token" }
                });

            if (isStudentRole)
            {
                var student = await _context.Students.FindAsync(id);

                if (student == null)
                    return NotFound(new ApiResponse<string>
                    {
                        StatusCode = 404,
                        IsSuccess = false,
                        ErrorMessage = { "Student not found" }
                    });

                var verifyResult = _studentPasswordHasher.VerifyHashedPassword(student, student.PasswordHash, changePasswordDto.CurrentPassword);
                if (verifyResult == PasswordVerificationResult.Failed)
                    return BadRequest(new ApiResponse<string>
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        ErrorMessage = { "Current password is incorrect" }
                    });

                var samePassword = _studentPasswordHasher.VerifyHashedPassword(student, student.PasswordHash, changePasswordDto.NewPassword);
                if (samePassword != PasswordVerificationResult.Failed)
                    return BadRequest(new ApiResponse<string>
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        ErrorMessage = { "New password must be different from current password" }
                    });

                student.PasswordHash = _studentPasswordHasher.HashPassword(student, changePasswordDto.NewPassword);
                student.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    ActionType.PASSWORD_CHANGE,
                    "Student",
                    student.StudentId.ToString(),
                    null,
                    null,
                    $"Student {student.Email} changed password",
                    student.StudentId.ToString(),
                    student.Email
                );

                return Ok(new ApiResponse<string>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Result = "Password changed successfully. Please login again."
                });
            }

            // Find user in database
            var user = await _context.ApplicationUsers.FindAsync(id);

            if (user == null)
                return NotFound(new ApiResponse<string>
                {
                    StatusCode = 404,
                    IsSuccess = false,
                    ErrorMessage = { "User not found" }
                });

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
                return BadRequest(new ApiResponse<string>
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    ErrorMessage = { "Current password is incorrect" }
                });

            // Check if new password is same as current password
            if (BCrypt.Net.BCrypt.Verify(changePasswordDto.NewPassword, user.PasswordHash))
                return BadRequest(new ApiResponse<string>
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    ErrorMessage = { "New password must be different from current password" }
                });

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            // Invalidate refresh token (force re-login on other devices)
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _context.SaveChangesAsync();

            // Log password change
            await _auditService.LogAsync(
                ActionType.PASSWORD_CHANGE,
                "User",
                user.Id.ToString(),
                null,
                null,
                $"User {user.Username} changed their password",
                user.Id.ToString(),
                user.Email
            );

            return Ok(new ApiResponse<string>
            {
                StatusCode = 200,
                IsSuccess = true,
                Result = "Password changed successfully. Please login again."
            });
        }
    }
}
