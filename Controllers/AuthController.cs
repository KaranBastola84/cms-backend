using Microsoft.AspNetCore.Mvc;
using JWTAuthAPI.Data;
using JWTAuthAPI.Models;
using JWTAuthAPI.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JWTAuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            // Check if username already exists
            if (await _context.ApplicationUsers.AnyAsync(u => u.Username == registerDto.Username))
                return BadRequest(new { message = "Username already exists" });

            // Check if email already exists
            if (await _context.ApplicationUsers.AnyAsync(u => u.Email == registerDto.Email))
                return BadRequest(new { message = "Email already exists" });

            // Create new user
            var user = new ApplicationUser
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Role = Roles.User, // Default role
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ApplicationUsers.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully", userId = user.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            // Find user by username
            var user = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            // Check if user exists
            if (user == null)
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid username or password" }
                });

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid username or password" }
                });

            // Check if user is active
            if (!user.IsActive)
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Account is inactive. Please contact administrator." }
                });

            // Generate JWT tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken(user);

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Result = new
                {
                    refresh = refreshToken,
                    access = accessToken,
                    user = new { role = user.Role, username = user.Username }
                }
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(RefreshTokenDto refreshTokenDto)
        {
            // Validate the refresh token
            var principal = _jwtService.ValidateToken(refreshTokenDto.RefreshToken);

            if (principal == null)
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid or expired refresh token" }
                });

            // Check if it's actually a refresh token
            var tokenType = _jwtService.GetClaimValue(principal, "token_type");
            if (tokenType != "refresh")
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid token type. Expected refresh token" }
                });

            // Get user ID from token
            var userIdClaim = _jwtService.GetClaimValue(principal, ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid token claims" }
                });

            // Find user in database
            var user = await _context.ApplicationUsers.FindAsync(userId);

            if (user == null)
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "User not found" }
                });

            // Check if user is active
            if (!user.IsActive)
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Account is inactive" }
                });

            // Verify the refresh token matches the one in database
            if (user.RefreshToken != refreshTokenDto.RefreshToken)
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid refresh token" }
                });

            // Check if refresh token has expired
            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return Unauthorized(new ApiResponse<string>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    ErrorMessage = { "Refresh token has expired. Please log in again" }
                });

            // Generate new tokens
            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken(user);

            // Update refresh token in database
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Result = new
                {
                    access = newAccessToken,
                    refresh = newRefreshToken,
                    user = new { role = user.Role, username = user.Username }
                }
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenDto refreshTokenDto)
        {
            // Validate the refresh token
            var principal = _jwtService.ValidateToken(refreshTokenDto.RefreshToken);

            if (principal == null)
                return BadRequest(new ApiResponse<string>
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid token" }
                });

            // Get user ID from token
            var userIdClaim = _jwtService.GetClaimValue(principal, ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return BadRequest(new ApiResponse<string>
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    ErrorMessage = { "Invalid token claims" }
                });

            // Find user in database
            var user = await _context.ApplicationUsers.FindAsync(userId);

            if (user == null)
                return NotFound(new ApiResponse<string>
                {
                    StatusCode = 404,
                    IsSuccess = false,
                    ErrorMessage = { "User not found" }
                });

            // Clear refresh token
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<string>
            {
                StatusCode = 200,
                IsSuccess = true,
                Result = "Logged out successfully"
            });
        }

        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin(RegisterDto registerDto)
        {
            // Check if username already exists
            if (await _context.ApplicationUsers.AnyAsync(u => u.Username == registerDto.Username))
                return BadRequest(new ApiResponse<string>
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    ErrorMessage = { "Username already exists" }
                });

            // Check if email already exists
            if (await _context.ApplicationUsers.AnyAsync(u => u.Email == registerDto.Email))
                return BadRequest(new ApiResponse<string>
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    ErrorMessage = { "Email already exists" }
                });

            // Create new admin user
            var admin = new ApplicationUser
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Role = Roles.Admin, // Admin role
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ApplicationUsers.Add(admin);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Result = new { message = "Admin registered successfully", userId = admin.Id }
            });
        }
    }
}