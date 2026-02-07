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

            // Check if inquiry already converted
            if (inquiry.ConvertedToStudentId.HasValue)
            {
                return BadRequest(ResponseHelper.Error<object>("Cannot update status of converted inquiry. Inquiry already converted to student."));
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

            return Ok(ResponseHelper.Success(inquiry, "Inquiry status updated successfully"));
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

            // Check if inquiry already converted
            if (inquiry.ConvertedToStudentId.HasValue)
            {
                return BadRequest(ResponseHelper.Error<object>("Cannot assign converted inquiry. Inquiry already converted to student."));
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

            return Ok(ResponseHelper.Success(new
            {
                inquiry,
                assignedTo = new { assignedUser.Id, assignedUser.Username, assignedUser.Email }
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

            // Log follow-up note addition
            await _auditService.LogAsync(
                ActionType.CREATE,
                "FollowUpNote",
                followUpNote.Id.ToString(),
                null,
                System.Text.Json.JsonSerializer.Serialize(new { InquiryId = id, Note = noteDto.Note }),
                $"Follow-up note added to inquiry #{id}"
            );

            return Ok(ResponseHelper.Success(followUpNote, "Follow-up note added successfully"));
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

        // POST: api/inquiry/{id}/convert-to-student (Admin/Staff only - Convert inquiry to student)
        [HttpPost("{id}/convert-to-student")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> ConvertToStudent(int id, [FromBody] ConvertInquiryDto convertDto)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);

            if (inquiry == null)
            {
                return NotFound(ResponseHelper.Error<object>("Inquiry not found", 404));
            }

            // Check if inquiry already converted
            if (inquiry.ConvertedToStudentId.HasValue)
            {
                return BadRequest(ResponseHelper.Error<object>($"Inquiry already converted to student (ID: {inquiry.ConvertedToStudentId})"));
            }

            // Check if email already exists as student
            var existingStudent = await _context.Students.FirstOrDefaultAsync(s => s.Email == inquiry.Email);
            if (existingStudent != null)
            {
                return BadRequest(ResponseHelper.Error<object>("A student with this email already exists"));
            }

            // Validate CourseId if provided
            if (convertDto.CourseId.HasValue)
            {
                var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == convertDto.CourseId.Value);
                if (!courseExists)
                {
                    return BadRequest(ResponseHelper.Error<object>($"Course with ID {convertDto.CourseId} does not exist"));
                }
            }

            // Validate BatchId if provided
            if (convertDto.BatchId.HasValue)
            {
                var batchExists = await _context.Batches.AnyAsync(b => b.BatchId == convertDto.BatchId.Value);
                if (!batchExists)
                {
                    return BadRequest(ResponseHelper.Error<object>($"Batch with ID {convertDto.BatchId} does not exist"));
                }
            }

            // Generate secure random password if not provided
            string password;
            if (string.IsNullOrEmpty(convertDto.Password))
            {
                // Generate random 12-character password
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
                var random = new Random();
                password = new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
            }
            else
            {
                password = convertDto.Password;
            }

            // Create student from inquiry data
            var student = new Student
            {
                Name = inquiry.FullName,
                Email = inquiry.Email,
                Phone = inquiry.PhoneNumber,
                CourseId = convertDto.CourseId,
                BatchId = convertDto.BatchId,
                Status = StudentStatus.Enrolled,
                AdmissionDate = convertDto.AdmissionDate ?? DateTime.UtcNow,
                FeesTotal = convertDto.FeesTotal ?? 0,
                FeesPaid = convertDto.FeesPaid ?? 0,
                Address = convertDto.Address,
                EmergencyContact = convertDto.EmergencyContact,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Update inquiry with conversion info
            inquiry.ConvertedToStudentId = student.StudentId;
            inquiry.ConvertedAt = DateTime.UtcNow;
            inquiry.Status = InquiryStatus.Enrolled;
            inquiry.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Log conversion
            await _auditService.LogAsync(
                ActionType.CREATE,
                "Student",
                student.StudentId.ToString(),
                System.Text.Json.JsonSerializer.Serialize(new { InquiryId = inquiry.Id }),
                System.Text.Json.JsonSerializer.Serialize(new { student.Name, student.Email }),
                $"Student created from inquiry #{inquiry.Id}"
            );

            return Ok(ResponseHelper.Success(new
            {
                student,
                inquiryId = inquiry.Id,
                temporaryPassword = string.IsNullOrEmpty(convertDto.Password) ? password : null // Return temp password only if auto-generated
            }, "Inquiry successfully converted to student"));
        }

        // GET: api/inquiry/analytics (Admin/Staff only - Get inquiry analytics)
        [HttpGet("analytics")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetInquiryAnalytics()
        {
            var totalInquiries = await _context.Inquiries.CountAsync();
            var convertedInquiries = await _context.Inquiries.CountAsync(i => i.ConvertedToStudentId.HasValue);

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

            var conversionRate = totalInquiries > 0 ? (convertedInquiries * 100.0 / totalInquiries) : 0;

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
                convertedInquiries,
                conversionRate = Math.Round(conversionRate, 2),
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

            // Prevent deletion of converted inquiries
            if (inquiry.ConvertedToStudentId.HasValue)
            {
                return BadRequest(ResponseHelper.Error<object>(
                    $"Cannot delete converted inquiry. This inquiry was converted to student (ID: {inquiry.ConvertedToStudentId}). Delete the student record first if needed."));
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

    // DTO for converting inquiry to student
    public class ConvertInquiryDto
    {
        public int? CourseId { get; set; }
        public int? BatchId { get; set; }
        public string? Address { get; set; }
        public string? EmergencyContact { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public decimal? FeesTotal { get; set; }
        public decimal? FeesPaid { get; set; }
        public string? Password { get; set; } // Optional - will use default if not provided
    }
}
