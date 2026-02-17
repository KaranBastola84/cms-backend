using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JWTAuthAPI.Data;
using JWTAuthAPI.Models;
using JWTAuthAPI.Helpers;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace JWTAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InquiryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly Services.IEmailService _emailService;
        private readonly Services.IAuditService _auditService;

        public InquiryController(ApplicationDbContext context, Services.IEmailService emailService, Services.IAuditService auditService)
        {
            _context = context;
            _emailService = emailService;
            _auditService = auditService;
        }

        // POST: api/inquiry (Public - No authentication required)
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitInquiry([FromBody] InquiryDto inquiryDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseHelper.Error<object>("Invalid inquiry data"));
            }

            var inquiry = new Inquiry
            {
                FullName = inquiryDto.FullName,
                Email = inquiryDto.Email,
                PhoneNumber = inquiryDto.PhoneNumber,
                Message = inquiryDto.Message,
                CourseInterest = inquiryDto.CourseInterest,
                Status = InquiryStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Inquiries.Add(inquiry);
            await _context.SaveChangesAsync();

            // Log inquiry submission
            await _auditService.LogAsync(
                ActionType.CREATE,
                "Inquiry",
                inquiry.Id.ToString(),
                null,
                System.Text.Json.JsonSerializer.Serialize(new { inquiry.FullName, inquiry.Email, inquiry.CourseInterest }),
                "Inquiry submitted by visitor"
            );

            // Send confirmation email (fire and forget - don't wait for it to complete)
            _ = _emailService.SendInquiryConfirmationEmailAsync(inquiry.Email, inquiry.FullName);

            return Ok(ResponseHelper.Success(new
            {
                inquiryId = inquiry.Id,
                submittedAt = inquiry.CreatedAt
            }, "Inquiry submitted successfully! We will contact you soon."));
        }

        // GET: api/inquiry (Admin/Staff only - View all inquiries)
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAllInquiries(
            [FromQuery] InquiryStatus? status = null,
            [FromQuery] int? assignedToId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Max page size limit

            var query = _context.Inquiries
                .Include(i => i.AssignedTo)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(i => i.Status == status.Value);
            }

            if (assignedToId.HasValue)
            {
                query = query.Where(i => i.AssignedToId == assignedToId.Value);
            }

            var totalCount = await query.CountAsync();
            var inquiries = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new
                {
                    i.Id,
                    i.FullName,
                    i.Email,
                    i.PhoneNumber,
                    i.Message,
                    i.CourseInterest,
                    i.Status,
                    i.CreatedAt,
                    i.ResponsedAt,
                    i.ResponseNotes,
                    i.UpdatedAt,
                    i.AssignedToId,
                    i.AssignedAt,
                    i.ConvertedToStudentId,
                    i.ConvertedAt,
                    assignedTo = i.AssignedTo != null ? new
                    {
                        i.AssignedTo.Id,
                        i.AssignedTo.Username,
                        i.AssignedTo.Email
                    } : null
                })
                .ToListAsync();

            return Ok(ResponseHelper.Success(new
            {
                inquiries,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            }, "Inquiries retrieved successfully"));
        }

        // GET: api/inquiry/{id} (Admin/Staff only)
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetInquiryById(int id)
        {
            var inquiry = await _context.Inquiries
                .Include(i => i.AssignedTo)
                .Where(i => i.Id == id)
                .Select(i => new
                {
                    i.Id,
                    i.FullName,
                    i.Email,
                    i.PhoneNumber,
                    i.Message,
                    i.CourseInterest,
                    i.Status,
                    i.CreatedAt,
                    i.ResponsedAt,
                    i.ResponseNotes,
                    i.UpdatedAt,
                    i.AssignedToId,
                    i.AssignedAt,
                    i.ConvertedToStudentId,
                    i.ConvertedAt,
                    assignedTo = i.AssignedTo != null ? new
                    {
                        i.AssignedTo.Id,
                        i.AssignedTo.Username,
                        i.AssignedTo.Email
                    } : null
                })
                .FirstOrDefaultAsync();

            if (inquiry == null)
            {
                return NotFound(ResponseHelper.Error<object>("Inquiry not found", 404));
            }

            return Ok(ResponseHelper.Success(inquiry, "Inquiry retrieved successfully"));
        }

        // PUT: api/inquiry/{id}/status (Admin/Staff only - Update inquiry status)
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateInquiryStatus(
            int id,
            [FromBody] UpdateInquiryStatusDto updateDto)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);

            if (inquiry == null)
            {
                return NotFound(ResponseHelper.Error<object>("Inquiry not found", 404));
            }

            // Store old values for audit log
            var oldStatus = inquiry.Status;
            var oldNotes = inquiry.ResponseNotes;

            inquiry.Status = updateDto.Status;
            inquiry.ResponseNotes = updateDto.ResponseNotes;
            inquiry.UpdatedAt = DateTime.UtcNow;

            if (inquiry.Status != InquiryStatus.Pending && inquiry.ResponsedAt == null)
            {
                inquiry.ResponsedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Log inquiry update
            await _auditService.LogAsync(
                ActionType.UPDATE,
                "Inquiry",
                inquiry.Id.ToString(),
                System.Text.Json.JsonSerializer.Serialize(new { Status = oldStatus.ToString(), ResponseNotes = oldNotes }),
                System.Text.Json.JsonSerializer.Serialize(new { Status = inquiry.Status.ToString(), inquiry.ResponseNotes }),
                $"Inquiry status updated from {oldStatus} to {inquiry.Status}"
            );

            // Return safe data only
            return Ok(ResponseHelper.Success(new
            {
                id = inquiry.Id,
                fullName = inquiry.FullName,
                email = inquiry.Email,
                status = inquiry.Status,
                responseNotes = inquiry.ResponseNotes,
                responsedAt = inquiry.ResponsedAt,
                updatedAt = inquiry.UpdatedAt
            }, "Inquiry status updated successfully"));
        }

        // PUT: api/inquiry/{id}/assign (Admin/Staff only - Assign inquiry to staff)
        [HttpPut("{id}/assign")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> AssignInquiry(int id, [FromBody] AssignInquiryDto assignDto)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);

            if (inquiry == null)
            {
                return NotFound(ResponseHelper.Error<object>("Inquiry not found", 404));
            }

            // Verify the assigned user exists and is Staff or Admin
            var assignedUser = await _context.ApplicationUsers.FindAsync(assignDto.AssignedToId);
            if (assignedUser == null)
            {
                return BadRequest(ResponseHelper.Error<object>("Assigned user not found"));
            }

            if (!assignedUser.IsActive)
            {
                return BadRequest(ResponseHelper.Error<object>("Cannot assign to inactive user"));
            }

            if (assignedUser.Role != Roles.Admin && assignedUser.Role != Roles.Staff)
            {
                return BadRequest(ResponseHelper.Error<object>("Can only assign to Admin or Staff users"));
            }

            var oldAssignedId = inquiry.AssignedToId;
            inquiry.AssignedToId = assignDto.AssignedToId;
            inquiry.AssignedAt = DateTime.UtcNow;
            inquiry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log inquiry assignment
            await _auditService.LogAsync(
                ActionType.UPDATE,
                "Inquiry",
                inquiry.Id.ToString(),
                System.Text.Json.JsonSerializer.Serialize(new { AssignedToId = oldAssignedId }),
                System.Text.Json.JsonSerializer.Serialize(new { AssignedToId = assignDto.AssignedToId }),
                $"Inquiry assigned to {assignedUser.Username}"
            );

            // Return safe data without sensitive fields
            return Ok(ResponseHelper.Success(new
            {
                inquiryId = inquiry.Id,
                fullName = inquiry.FullName,
                email = inquiry.Email,
                phoneNumber = inquiry.PhoneNumber,
                courseInterest = inquiry.CourseInterest,
                status = inquiry.Status,
                assignedToId = inquiry.AssignedToId,
                assignedAt = inquiry.AssignedAt,
                assignedTo = new
                {
                    id = assignedUser.Id,
                    username = assignedUser.Username,
                    email = assignedUser.Email
                }
            }, "Inquiry assigned successfully"));
        }

        // POST: api/inquiry/{id}/followup (Admin/Staff only - Add follow-up note)
        [HttpPost("{id}/followup")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> AddFollowUpNote(int id, [FromBody] AddFollowUpNoteDto noteDto)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);

            if (inquiry == null)
            {
                return NotFound(ResponseHelper.Error<object>("Inquiry not found", 404));
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(ResponseHelper.Error<object>("Invalid user"));
            }

            var followUpNote = new FollowUpNote
            {
                InquiryId = id,
                CreatedById = userId,
                Note = noteDto.Note,
                CreatedAt = DateTime.UtcNow
            };

            _context.FollowUpNotes.Add(followUpNote);
            await _context.SaveChangesAsync();

            // Reload with CreatedBy to include user details
            var createdNote = await _context.FollowUpNotes
                .Include(f => f.CreatedBy)
                .Where(f => f.Id == followUpNote.Id)
                .Select(f => new
                {
                    f.Id,
                    f.Note,
                    f.CreatedAt,
                    createdBy = new
                    {
                        f.CreatedBy!.Id,
                        f.CreatedBy.Username,
                        f.CreatedBy.Email
                    }
                })
                .FirstOrDefaultAsync();

            // Log follow-up note addition
            await _auditService.LogAsync(
                ActionType.CREATE,
                "FollowUpNote",
                followUpNote.Id.ToString(),
                null,
                System.Text.Json.JsonSerializer.Serialize(new { InquiryId = id, Note = noteDto.Note }),
                $"Follow-up note added to inquiry #{id}"
            );

            return Ok(ResponseHelper.Success(createdNote, "Follow-up note added successfully"));
        }

        // GET: api/inquiry/{id}/followup (Admin/Staff only - Get all follow-up notes)
        [HttpGet("{id}/followup")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetFollowUpNotes(int id)
        {
            var inquiryExists = await _context.Inquiries.AnyAsync(i => i.Id == id);

            if (!inquiryExists)
            {
                return NotFound(ResponseHelper.Error<object>("Inquiry not found", 404));
            }

            var followUpNotes = await _context.FollowUpNotes
                .Where(f => f.InquiryId == id)
                .Include(f => f.CreatedBy)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new
                {
                    f.Id,
                    f.Note,
                    f.CreatedAt,
                    createdBy = new
                    {
                        f.CreatedBy!.Id,
                        f.CreatedBy.Username,
                        f.CreatedBy.Email
                    }
                })
                .ToListAsync();

            return Ok(ResponseHelper.Success(followUpNotes, "Follow-up notes retrieved successfully"));
        }

        // GET: api/inquiry/analytics (Admin/Staff only - Get inquiry analytics)
        [HttpGet("analytics")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetInquiryAnalytics()
        {
            var totalInquiries = await _context.Inquiries.CountAsync();

            var inquiriesByStatus = await _context.Inquiries
                .GroupBy(i => i.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            var inquiriesByCourse = await _context.Inquiries
                .Where(i => i.CourseInterest != null)
                .GroupBy(i => i.CourseInterest)
                .Select(g => new { Course = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            // Inquiries over time (last 30 days)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var inquiriesOverTime = await _context.Inquiries
                .Where(i => i.CreatedAt >= thirtyDaysAgo)
                .GroupBy(i => i.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Recent inquiries (last 7 days)
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            var recentInquiries = await _context.Inquiries.CountAsync(i => i.CreatedAt >= sevenDaysAgo);

            var analytics = new
            {
                totalInquiries,
                recentInquiries = new
                {
                    last7Days = recentInquiries,
                    last30Days = await _context.Inquiries.CountAsync(i => i.CreatedAt >= thirtyDaysAgo)
                },
                inquiriesByStatus,
                inquiriesByCourse,
                inquiriesOverTime
            };

            return Ok(ResponseHelper.Success(analytics, "Analytics retrieved successfully"));
        }

        // DELETE: api/inquiry/{id} (Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteInquiry(int id)
        {
            var inquiry = await _context.Inquiries
                .Include(i => i.FollowUpNotes)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inquiry == null)
            {
                return NotFound(ResponseHelper.Error<object>("Inquiry not found", 404));
            }

            // Store inquiry data before deletion
            var inquiryData = System.Text.Json.JsonSerializer.Serialize(new { inquiry.FullName, inquiry.Email, inquiry.Status });

            _context.Inquiries.Remove(inquiry);
            await _context.SaveChangesAsync();

            // Log inquiry deletion
            await _auditService.LogAsync(
                ActionType.DELETE,
                "Inquiry",
                id.ToString(),
                inquiryData,
                null,
                $"Inquiry deleted for {inquiry.FullName}"
            );

            return Ok(ResponseHelper.Success(new { message = "Inquiry deleted successfully" }, "Inquiry deleted successfully"));
        }
    }

    // DTO for updating inquiry status
    public class UpdateInquiryStatusDto
    {
        public InquiryStatus Status { get; set; }
        public string? ResponseNotes { get; set; }
    }

    // DTO for assigning inquiry
    public class AssignInquiryDto
    {
        [Required(ErrorMessage = "AssignedToId is required")]
        public int AssignedToId { get; set; }
    }

    // DTO for adding follow-up note
    public class AddFollowUpNoteDto
    {
        [Required]
        [StringLength(1000, MinimumLength = 5)]
        public string Note { get; set; } = string.Empty;
    }
}
