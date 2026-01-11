using System.Threading.Tasks;
using JWTAuthAPI.Models;

namespace JWTAuthAPI.Services
{
    public interface IAuditService
    {
        Task LogAsync(
            ActionType actionType,
            string module,
            string entityId = "",
            string? previousValue = null,
            string? newValue = null,
            string? additionalInfo = null,
            string? userId = null,
            string? userEmail = null,
            string? ipAddress = null
        );

        Task<IEnumerable<AuditLog>> GetLogsAsync(int pageNumber = 1, int pageSize = 50);
        Task<IEnumerable<AuditLog>> GetLogsByUserAsync(string userId, int pageNumber = 1, int pageSize = 50);
        Task<IEnumerable<AuditLog>> GetLogsByModuleAsync(string module, int pageNumber = 1, int pageSize = 50);
    }
}
