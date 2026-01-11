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
    public class TrainerManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;

        public TrainerManagementController(
            ApplicationDbContext context,
            IEmailService emailService,
            IAuditService auditService)
        {
            _context = context;
            _emailService = emailService;
            _auditService = auditService;
        }

        /// <summary>
        /// Admin creates a new trainer account and sends OTP to trainer email
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateTrainer(CreateTrainerDto dto)
        {
            // Check if email already exists
            if (await _context.ApplicationUsers.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest(ResponseHelper.Error<object>("Email already exists"));
            }

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var otpExpiry = DateTime.UtcNow.AddMinutes(15);

            // Create trainer user with pending status
            var trainer = new ApplicationUser
            {
                Username = dto.Email, // Username is the email
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                TrainerRole = dto.TrainerRole,
                Role = Roles.Trainer,
                PasswordHash = string.Empty, // Password will be set after OTP verification
                IsActive = false, // Inactive until admin activates
                IsVerified = false, // Not verified until OTP is entered
                OTP = otp,
                OTPExpiry = otpExpiry,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ApplicationUsers.Add(trainer);
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
                _context.ApplicationUsers.Remove(trainer);
                await _context.SaveChangesAsync();
                return StatusCode(500, ResponseHelper.Error<object>($"Failed to send OTP email: {ex.Message}", 500));
            }

            // Log trainer creation
            await _auditService.LogAsync(
                ActionType.CREATE,
                "Trainer",
                trainer.Id.ToString(),
                null,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    trainer.Email,
                    trainer.FirstName,
                    trainer.LastName,
                    trainer.TrainerRole
                }),
                $"Trainer account created for {trainer.Email}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            );

            return Ok(ResponseHelper.Success(new
            {
                message = "Trainer account created successfully. OTP has been sent to the trainer email.",
                trainerId = trainer.Id,
                email = trainer.Email
            }));
        }

        /// <summary>
        /// Trainer verifies OTP and sets their password (No authorization required)
        /// </summary>
        [HttpPost("verify-otp")]
        [AllowAnonymous] // Trainer can access this without being logged in
        public async Task<IActionResult> VerifyOTP(VerifyTrainerOTPDto dto)
        {
            var trainer = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Role == Roles.Trainer);

            if (trainer == null)
            {
                return NotFound(ResponseHelper.Error<object>("Trainer account not found"));
            }

            if (trainer.IsVerified)
            {
                return BadRequest(ResponseHelper.Error<object>("Account is already verified"));
            }

            if (string.IsNullOrEmpty(trainer.OTP) || trainer.OTP != dto.OTP)
            {
                return BadRequest(ResponseHelper.Error<object>("Invalid OTP"));
            }

            if (trainer.OTPExpiry == null || trainer.OTPExpiry < DateTime.UtcNow)
            {
                return BadRequest(ResponseHelper.Error<object>("OTP has expired. Please contact administrator."));
            }

            // Set password and mark as verified
            trainer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            trainer.IsVerified = true;
            trainer.OTP = null; // Clear OTP
            trainer.OTPExpiry = null;
            trainer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log verification
            await _auditService.LogAsync(
                ActionType.UPDATE,
                "Trainer",
                trainer.Id.ToString(),
                null,
                null,
                $"Trainer {trainer.Email} verified OTP and set password",
                trainer.Id.ToString(),
                trainer.Email
            );

            return Ok(ResponseHelper.Success(new
            {
                message = "OTP verified successfully. Please wait for administrator to activate your account.",
                isVerified = true,
                isActive = trainer.IsActive
            }));
        }

        /// <summary>
        /// Admin activates a trainer account
        /// </summary>
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateTrainer(int id)
        {
            var trainer = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == Roles.Trainer);

            if (trainer == null)
            {
                return NotFound(ResponseHelper.Error<object>("Trainer not found"));
            }

            if (!trainer.IsVerified)
            {
                return BadRequest(ResponseHelper.Error<object>("Trainer must verify their email with OTP before activation"));
            }

            if (trainer.IsActive)
            {
                return BadRequest(ResponseHelper.Error<object>("Trainer account is already active"));
            }

            trainer.IsActive = true;
            trainer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Send activation email
            try
            {
                await _emailService.SendAccountActivationEmailAsync(
                    trainer.Email,
                    $"{trainer.FirstName} {trainer.LastName}",
                    trainer.Email
                );
            }
            catch (Exception ex)
            {
                // Log but don't fail activation if email fails
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Trainer",
                    trainer.Id.ToString(),
                    null,
                    null,
                    $"Failed to send activation email to {trainer.Email}: {ex.Message}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );
            }

            // Log activation
            await _auditService.LogAsync(
                ActionType.UPDATE,
                "Trainer",
                trainer.Id.ToString(),
                System.Text.Json.JsonSerializer.Serialize(new { IsActive = false }),
                System.Text.Json.JsonSerializer.Serialize(new { IsActive = true }),
                $"Trainer account activated for {trainer.Email}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            );

            return Ok(ResponseHelper.Success(new
            {
                message = "Trainer account activated successfully. Activation email sent.",
                trainerId = trainer.Id,
                email = trainer.Email
            }));
        }

        /// <summary>
        /// Admin deactivates a trainer account
        /// </summary>
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateTrainer(int id)
        {
            var trainer = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == Roles.Trainer);

            if (trainer == null)
            {
                return NotFound(ResponseHelper.Error<object>("Trainer not found"));
            }

            if (!trainer.IsActive)
            {
                return BadRequest(ResponseHelper.Error<object>("Trainer account is already inactive"));
            }

            trainer.IsActive = false;
            trainer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Log deactivation
            await _auditService.LogAsync(
                ActionType.UPDATE,
                "Trainer",
                trainer.Id.ToString(),
                System.Text.Json.JsonSerializer.Serialize(new { IsActive = true }),
                System.Text.Json.JsonSerializer.Serialize(new { IsActive = false }),
                $"Trainer account deactivated for {trainer.Email}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            );

            return Ok(ResponseHelper.Success(new
            {
                message = "Trainer account deactivated successfully",
                trainerId = trainer.Id
            }));
        }

        /// <summary>
        /// Admin gets list of all trainers
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllTrainers()
        {
            var trainerList = await _context.ApplicationUsers
                .Where(u => u.Role == Roles.Trainer)
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new TrainerListItemDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName ?? string.Empty,
                    LastName = u.LastName ?? string.Empty,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber ?? string.Empty,
                    TrainerRole = u.TrainerRole ?? string.Empty,
                    IsVerified = u.IsVerified,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(ResponseHelper.Success(trainerList));
        }

        /// <summary>
        /// Admin gets details of a specific trainer
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTrainerById(int id)
        {
            var trainer = await _context.ApplicationUsers
                .Where(u => u.Id == id && u.Role == Roles.Trainer)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.PhoneNumber,
                    u.TrainerRole,
                    u.IsVerified,
                    u.IsActive,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (trainer == null)
            {
                return NotFound(ResponseHelper.Error<object>("Trainer not found"));
            }

            return Ok(ResponseHelper.Success(trainer));
        }

        /// <summary>
        /// Admin resends OTP to trainer email
        /// </summary>
        [HttpPost("{id}/resend-otp")]
        public async Task<IActionResult> ResendOTP(int id)
        {
            var trainer = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == Roles.Trainer);

            if (trainer == null)
            {
                return NotFound(ResponseHelper.Error<object>("Trainer not found"));
            }

            if (trainer.IsVerified)
            {
                return BadRequest(ResponseHelper.Error<object>("Trainer account is already verified."));
            }

            // Generate new OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var otpExpiry = DateTime.UtcNow.AddMinutes(15);

            trainer.OTP = otp;
            trainer.OTPExpiry = otpExpiry;
            trainer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send OTP email
            try
            {
                await _emailService.SendOTPEmailAsync(
                    trainer.Email,
                    $"{trainer.FirstName} {trainer.LastName}",
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
                "Trainer",
                trainer.Id.ToString(),
                null,
                null,
                $"OTP resent to {trainer.Email}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            );

            return Ok(ResponseHelper.Success(new
            {
                message = "OTP has been resent to the trainer email",
                email = trainer.Email
            }));
        }

        /// <summary>
        /// Admin deletes a trainer account
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTrainer(int id)
        {
            var trainer = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == Roles.Trainer);

            if (trainer == null)
            {
                return NotFound(ResponseHelper.Error<object>("Trainer not found"));
            }

            // Log deletion before removing
            await _auditService.LogAsync(
                ActionType.DELETE,
                "Trainer",
                trainer.Id.ToString(),
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    trainer.Email,
                    trainer.FirstName,
                    trainer.LastName
                }),
                null,
                $"Trainer account deleted for {trainer.Email}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            );

            _context.ApplicationUsers.Remove(trainer);
            await _context.SaveChangesAsync();

            return Ok(ResponseHelper.Success(new
            {
                message = "Trainer account deleted successfully"
            }));
        }

        /// <summary>
        /// Trainer requests password change - OTP sent to email (Trainer can access this)
        /// </summary>
        [HttpPost("request-password-change")]
        [Authorize(Roles = Roles.Trainer)] // Only trainer can access
        public async Task<IActionResult> RequestPasswordChange()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ResponseHelper.Error<object>("User not authenticated"));
            }

            var trainer = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId && u.Role == Roles.Trainer);

            if (trainer == null)
            {
                return NotFound(ResponseHelper.Error<object>("Trainer not found"));
            }

            if (!trainer.IsActive)
            {
                return BadRequest(ResponseHelper.Error<object>("Trainer account is not active"));
            }

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var otpExpiry = DateTime.UtcNow.AddMinutes(15);

            trainer.OTP = otp;
            trainer.OTPExpiry = otpExpiry;
            trainer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send OTP email
            try
            {
                await _emailService.SendOTPEmailAsync(
                    trainer.Email,
                    $"{trainer.FirstName} {trainer.LastName}",
                    otp
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Failed to send OTP email: {ex.Message}", 500));
            }

            // Log password change request
            await _auditService.LogAsync(
                ActionType.UPDATE,
                "Trainer",
                trainer.Id.ToString(),
                null,
                null,
                $"Password change OTP sent to {trainer.Email}",
                trainer.Id.ToString(),
                trainer.Email
            );

            return Ok(ResponseHelper.Success(new
            {
                message = "OTP has been sent to your email for password verification",
                email = trainer.Email
            }));
        }

        /// <summary>
        /// Trainer verifies OTP and changes password (Trainer can access this)
        /// </summary>
        [HttpPost("verify-password-change")]
        [Authorize(Roles = Roles.Trainer)] // Only trainer can access
        public async Task<IActionResult> VerifyPasswordChange(VerifyPasswordChangeDto dto)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ResponseHelper.Error<object>("User not authenticated"));
            }

            var trainer = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId && u.Role == Roles.Trainer);

            if (trainer == null)
            {
                return NotFound(ResponseHelper.Error<object>("Trainer not found"));
            }

            if (!trainer.IsActive)
            {
                return BadRequest(ResponseHelper.Error<object>("Trainer account is not active"));
            }

            if (string.IsNullOrEmpty(trainer.OTP) || trainer.OTP != dto.OTP)
            {
                return BadRequest(ResponseHelper.Error<object>("Invalid OTP"));
            }

            if (trainer.OTPExpiry == null || trainer.OTPExpiry < DateTime.UtcNow)
            {
                return BadRequest(ResponseHelper.Error<object>("OTP has expired. Please request a new one."));
            }

            // Update password
            trainer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            trainer.OTP = null; // Clear OTP
            trainer.OTPExpiry = null;
            trainer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log password change
            await _auditService.LogAsync(
                ActionType.UPDATE,
                "Trainer",
                trainer.Id.ToString(),
                null,
                null,
                $"Password changed successfully for {trainer.Email}",
                trainer.Id.ToString(),
                trainer.Email
            );

            return Ok(ResponseHelper.Success(new
            {
                message = "Password changed successfully"
            }));
        }
    }
}
