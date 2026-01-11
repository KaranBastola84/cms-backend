using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JWTAuthAPI.Data;
using JWTAuthAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace JWTAuthAPI.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            ActionType actionType,
            string module,
            string entityId = "",
            string? previousValue = null,
            string? newValue = null,
            string? additionalInfo = null,
            string? userId = null,
            string? userEmail = null,
            string? ipAddress = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                // Get user info from context if not provided
                if (userId == null && httpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    userEmail = httpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? "";
                }

                // Get IP address if not provided
                if (ipAddress == null && httpContext != null)
                {
                    ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                }

                var auditLog = new AuditLog
                {
                    UserId = userId,
                    UserEmail = userEmail ?? "Anonymous",
                    ActionType = actionType,
                    Module = module,
                    EntityId = entityId,
                    PreviousValue = previousValue,
                    NewValue = newValue,
                    AdditionalInfo = additionalInfo,
                    IpAddress = ipAddress ?? "Unknown",
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log to console or external logging service
                // Don't throw - audit failures shouldn't break the main operation
                Console.WriteLine($"Audit logging failed: {ex.Message}");
            }
        }

        public async Task<IEnumerable<AuditLog>> GetLogsAsync(int pageNumber = 1, int pageSize = 50)
        {
            return await _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetLogsByUserAsync(string userId, int pageNumber = 1, int pageSize = 50)
        {
            return await _context.AuditLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetLogsByModuleAsync(string module, int pageNumber = 1, int pageSize = 50)
        {
            return await _context.AuditLogs
                .Where(a => a.Module == module)
                .OrderByDescending(a => a.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
