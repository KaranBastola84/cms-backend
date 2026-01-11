using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JWTAuthAPI.Data;
using JWTAuthAPI.Models;
using JWTAuthAPI.Services;
using JWTAuthAPI.Helpers;

namespace JWTAuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Admin)] // All endpoints require Admin role
    public class StaffManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;

        public StaffManagementController(
            ApplicationDbContext context,
            IEmailService emailService,
            IAuditService auditService)
        {
            _context = context;
            _emailService = emailService;
            _auditService = auditService;
        }

        /// <summary>
        /// Admin creates a new staff account and sends OTP to staff email
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateStaff(CreateStaffDto dto)
        {
            // Check if email already exists
            if (await _context.ApplicationUsers.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest(ResponseHelper.Error<object>("Email already exists"));
            }

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var otpExpiry = DateTime.UtcNow.AddMinutes(15);

            // Create staff user with pending status
            var staff = new ApplicationUser
            {
                Username = dto.Email, // Username is the email
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                StaffRole = dto.StaffRole,
                Role = Roles.Staff,
                PasswordHash = string.Empty, // Password will be set after OTP verification
                IsActive = false, // Inactive until admin activates
                IsVerified = false, // Not verified until OTP is entered
                OTP = otp,
                OTPExpiry = otpExpiry,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ApplicationUsers.Add(staff);
            await _context.SaveChangesAsync();

            // Send OTP email
            try
            {
                await _emailService.SendOTPEmailAsync(
                    dto.Email,
                    $"{dto.FirstName} {dto.LastName}",
                    otp
                );
            }
            catch (Exception ex)
            {
                // Rollback user creation if email fails
                _context.ApplicationUsers.Remove(staff);
                await _context.SaveChangesAsync();
                return StatusCode(500, ResponseHelper.Error<object>($"Failed to send OTP email: {ex.Message}", 500));
            }

            // Log staff creation
            await _auditService.LogAsync(
                ActionType.CREATE,
                "Staff",
                staff.Id.ToString(),
                null,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    staff.Email,
                    staff.FirstName,
                    staff.LastName,
                    staff.StaffRole
                }),
                $"Staff account created for {staff.Email}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            );

            return Ok(ResponseHelper.Success(new
            {
                message = "Staff account created successfully. OTP has been sent to the staff email.",
                staffId = staff.Id,
                email = staff.Email
            }));
        }

        /// <summary>
        /// Staff verifies OTP and sets their password (No authorization required)
        /// </summary>
        [HttpPost("verify-otp")]
        [AllowAnonymous] // Staff can access this without being logged in
        public async Task<IActionResult> VerifyOTP(VerifyOTPDto dto)
        {
            var staff = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Role == Roles.Staff);

            if (staff == null)
            {
                return NotFound(ResponseHelper.Error<object>("Staff account not found"));
            }

            if (staff.IsVerified)
            {
                return BadRequest(ResponseHelper.Error<object>("Account is already verified"));
            }

            if (string.IsNullOrEmpty(staff.OTP) || staff.OTP != dto.OTP)
            {
                return BadRequest(ResponseHelper.Error<object>("Invalid OTP"));
            }

            if (staff.OTPExpiry == null || staff.OTPExpiry < DateTime.UtcNow)
            {
                return BadRequest(ResponseHelper.Error<object>("OTP has expired. Please contact administrator."));
            }

            // Set password and mark as verified
            staff.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            staff.IsVerified = true;
            staff.OTP = null; // Clear OTP
            staff.OTPExpiry = null;
            staff.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log verification
            await _auditService.LogAsync(
                ActionType.UPDATE,
                "Staff",
                staff.Id.ToString(),
                null,
                null,
                $"Staff {staff.Email} verified OTP and set password",
                staff.Id.ToString(),
                staff.Email
            );

            return Ok(ResponseHelper.Success(new
            {
                message = "OTP verified successfully. Please wait for administrator to activate your account.",
                isVerified = true,
                isActive = staff.IsActive
            }));
        }

        /// <summary>
        /// Admin activates a staff account
        /// </summary>
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateStaff(int id)
        {
            var staff = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == Roles.Staff);

            if (staff == null)
            {
                return NotFound(ResponseHelper.Error<object>("Staff not found"));
            }

            if (!staff.IsVerified)
            {
                return BadRequest(ResponseHelper.Error<object>("Staff must verify their email with OTP before activation"));
            }

            if (staff.IsActive)
            {
                return BadRequest(ResponseHelper.Error<object>("Staff account is already active"));
            }

            staff.IsActive = true;
            staff.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Send activation email
            try
            {
                await _emailService.SendAccountActivationEmailAsync(
                    staff.Email,
                    $"{staff.FirstName} {staff.LastName}",
                    staff.Email
                );
            }
            catch (Exception ex)
            {
                // Log but don't fail activation if email fails
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Staff",
                    staff.Id.ToString(),
                    null,
                    null,
                    $"Failed to send activation email to {staff.Email}: {ex.Message}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );
            }

            // Log activation
            await _auditService.LogAsync(
                ActionType.UPDATE,
                "Staff",
                staff.Id.ToString(),
                System.Text.Json.JsonSerializer.Serialize(new { IsActive = false }),
                System.Text.Json.JsonSerializer.Serialize(new { IsActive = true }),
                $"Staff account activated for {staff.Email}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            );

            return Ok(ResponseHelper.Success(new
            {
                message = "Staff account activated successfully. Activation email sent.",
                staffId = staff.Id,
                email = staff.Email
            }));
        }

        /// <summary>
        /// Admin deactivates a staff account
        /// </summary>
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateStaff(int id)
        {
            var staff = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == Roles.Staff);

            if (staff == null)
            {
                return NotFound(ResponseHelper.Error<object>("Staff not found"));
            }

            if (!staff.IsActive)
            {
                return BadRequest(ResponseHelper.Error<object>("Staff account is already inactive"));
            }

            staff.IsActive = false;
            staff.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Log deactivation
            await _auditService.LogAsync(
                ActionType.UPDATE,
                "Staff",
                staff.Id.ToString(),
                System.Text.Json.JsonSerializer.Serialize(new { IsActive = true }),
                System.Text.Json.JsonSerializer.Serialize(new { IsActive = false }),
                $"Staff account deactivated for {staff.Email}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            );

            return Ok(ResponseHelper.Success(new
            {
                message = "Staff account deactivated successfully",
                staffId = staff.Id
            }));
        }

        /// <summary>
        /// Admin gets list of all staff
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllStaff()
        {
            var staffList = await _context.ApplicationUsers
                .Where(u => u.Role == Roles.Staff)
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new StaffListItemDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName ?? string.Empty,
                    LastName = u.LastName ?? string.Empty,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber ?? string.Empty,
                    StaffRole = u.StaffRole ?? string.Empty,
                    IsVerified = u.IsVerified,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(ResponseHelper.Success(staffList));
        }

        /// <summary>
        /// Admin gets details of a specific staff member
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStaffById(int id)
        {
            var staff = await _context.ApplicationUsers
                .Where(u => u.Id == id && u.Role == Roles.Staff)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.PhoneNumber,
                    u.StaffRole,
                    u.IsVerified,
                    u.IsActive,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (staff == null)
            {
                return NotFound(ResponseHelper.Error<object>("Staff not found"));
            }

            return Ok(ResponseHelper.Success(staff));
        }

        /// <summary>
        /// Admin resends OTP to staff email
        /// </summary>
        [HttpPost("{id}/resend-otp")]
        public async Task<IActionResult> ResendOTP(int id)
        {
            var staff = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == Roles.Staff);

            if (staff == null)
            {
                return NotFound(ResponseHelper.Error<object>("Staff not found"));
            }

            if (staff.IsVerified && staff.IsActive)
            {
                return BadRequest(ResponseHelper.Error<object>("Staff account is already verified"));
            }

            // Generate new OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var otpExpiry = DateTime.UtcNow.AddMinutes(15);

            staff.OTP = otp;
            staff.OTPExpiry = otpExpiry;
            staff.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send OTP email
            try
            {
                await _emailService.SendOTPEmailAsync(
                    staff.Email,
                    $"{staff.FirstName} {staff.LastName}",
                    otp
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Failed to send OTP email: {ex.Message}", 500));
            }

            // Log OTP resend
            await _auditService.LogAsync(
                ActionType.UPDATE,
                "Staff",
                staff.Id.ToString(),
                null,
                null,
                $"OTP resent to {staff.Email}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            );

            return Ok(ResponseHelper.Success(new
            {
                message = "OTP has been resent to the staff email",
                email = staff.Email
            }));
        }

        /// <summary>
        /// Admin deletes a staff account
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            var staff = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == Roles.Staff);

            if (staff == null)
            {
                return NotFound(ResponseHelper.Error<object>("Staff not found"));
            }

            // Log deletion before removing
            await _auditService.LogAsync(
                ActionType.DELETE,
                "Staff",
                staff.Id.ToString(),
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    staff.Email,
                    staff.FirstName,
                    staff.LastName
                }),
                null,
                $"Staff account deleted for {staff.Email}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            );

            _context.ApplicationUsers.Remove(staff);
            await _context.SaveChangesAsync();

            return Ok(ResponseHelper.Success(new
            {
                message = "Staff account deleted successfully"
            }));
        }
    }
}
