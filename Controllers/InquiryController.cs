using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JWTAuthAPI.Data;
using JWTAuthAPI.Models;
using JWTAuthAPI.Helpers;
using Microsoft.EntityFrameworkCore;

namespace JWTAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InquiryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly Services.IEmailService _emailService;

        public InquiryController(ApplicationDbContext context, Services.IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Inquiries.AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(i => i.Status == status.Value);
            }

            var totalCount = await query.CountAsync();
            var inquiries = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
            var inquiry = await _context.Inquiries.FindAsync(id);

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

            inquiry.Status = updateDto.Status;
            inquiry.ResponseNotes = updateDto.ResponseNotes;

            if (inquiry.Status != InquiryStatus.Pending && inquiry.ResponsedAt == null)
            {
                inquiry.ResponsedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(ResponseHelper.Success(inquiry, "Inquiry status updated successfully"));
        }        // DELETE: api/inquiry/{id} (Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteInquiry(int id)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);

            if (inquiry == null)
            {
                return NotFound(ResponseHelper.Error<object>("Inquiry not found", 404));
            }

            _context.Inquiries.Remove(inquiry);
            await _context.SaveChangesAsync();

            return Ok(ResponseHelper.Success(new { message = "Inquiry deleted successfully" }, "Inquiry deleted successfully"));
        }
    }

    // DTO for updating inquiry status
    public class UpdateInquiryStatusDto
    {
        public InquiryStatus Status { get; set; }
        public string? ResponseNotes { get; set; }
    }
}
