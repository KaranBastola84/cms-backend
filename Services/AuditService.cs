using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JWTAuthAPI.Data;
using JWTAuthAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

        public async Task<byte[]> ExportAllLogsPdfAsync()
        {
            var logs = await _context.AuditLogs
                .OrderBy(a => a.Timestamp)
                .ToListAsync();

            var generatedAt = DateTime.UtcNow;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(18);

                    page.Header().Column(column =>
                    {
                        column.Item().Text("Audit Logs Export")
                            .FontSize(18)
                            .SemiBold();
                        column.Item().Text($"Generated (UTC): {generatedAt:yyyy-MM-dd HH:mm:ss} | Total Logs: {logs.Count}")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(8).Element(content =>
                    {
                        if (!logs.Any())
                        {
                            content.AlignCenter().AlignMiddle().Text("No audit logs available.").FontSize(12);
                            return;
                        }

                        content.Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(35);  // ID
                                columns.ConstantColumn(95);  // Timestamp
                                columns.ConstantColumn(75);  // Action
                                columns.ConstantColumn(80);  // Module
                                columns.ConstantColumn(50);  // Entity
                                columns.ConstantColumn(95);  // User
                                columns.RelativeColumn(1.2f); // Additional
                                columns.RelativeColumn(1);   // IP
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCellStyle).Text("ID").SemiBold().FontSize(9);
                                header.Cell().Element(HeaderCellStyle).Text("Timestamp").SemiBold().FontSize(9);
                                header.Cell().Element(HeaderCellStyle).Text("Action").SemiBold().FontSize(9);
                                header.Cell().Element(HeaderCellStyle).Text("Module").SemiBold().FontSize(9);
                                header.Cell().Element(HeaderCellStyle).Text("Entity").SemiBold().FontSize(9);
                                header.Cell().Element(HeaderCellStyle).Text("User").SemiBold().FontSize(9);
                                header.Cell().Element(HeaderCellStyle).Text("Info").SemiBold().FontSize(9);
                                header.Cell().Element(HeaderCellStyle).Text("IP").SemiBold().FontSize(9);
                            });

                            foreach (var log in logs)
                            {
                                table.Cell().Element(DataCellStyle).Text(log.LogId.ToString()).FontSize(8);
                                table.Cell().Element(DataCellStyle).Text(log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")).FontSize(8);
                                table.Cell().Element(DataCellStyle).Text(log.ActionType.ToString()).FontSize(8);
                                table.Cell().Element(DataCellStyle).Text(Compact(log.Module, 30)).FontSize(8);
                                table.Cell().Element(DataCellStyle).Text(Compact(log.EntityId, 20)).FontSize(8);
                                table.Cell().Element(DataCellStyle).Text(Compact(log.UserEmail, 40)).FontSize(8);
                                table.Cell().Element(DataCellStyle).Text(Compact(log.AdditionalInfo, 140)).FontSize(8);
                                table.Cell().Element(DataCellStyle).Text(Compact(log.IpAddress, 35)).FontSize(8);
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ").FontSize(9);
                        text.CurrentPageNumber().FontSize(9);
                        text.Span(" / ").FontSize(9);
                        text.TotalPages().FontSize(9);
                    });
                });
            }).GeneratePdf();
        }

        private static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten3)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .PaddingVertical(4)
                .PaddingHorizontal(3);
        }

        private static IContainer DataCellStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(3)
                .PaddingHorizontal(3);
        }

        private static string Compact(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "-";

            var clean = value.Replace("\r", " ").Replace("\n", " ").Trim();
            return clean.Length <= maxLength ? clean : clean.Substring(0, maxLength - 3) + "...";
        }
    }
}
