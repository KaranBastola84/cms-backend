using JWTAuthAPI.Models;
using JWTAuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace JWTAuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IFileService _fileService;

        public StudentController(
            IStudentService studentService,
            IStripePaymentService stripePaymentService,
            IFileService fileService)
        {
            _studentService = studentService;
            _stripePaymentService = stripePaymentService;
            _fileService = fileService;
        }

        [HttpPost]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _studentService.CreateStudentAsync(createDto, userId);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudentById(int id)
        {
            var result = await _studentService.GetStudentByIdAsync(id);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStudents()
        {
            var result = await _studentService.GetAllStudentsAsync();
            return Ok(result);
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetStudentsByStatus(StudentStatus status)
        {
            var result = await _studentService.GetStudentsByStatusAsync(status);
            return Ok(result);
        }

        [HttpGet("email/{email}")]
        public async Task<IActionResult> GetStudentByEmail(string email)
        {
            var result = await _studentService.GetStudentByEmailAsync(email);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _studentService.UpdateStudentAsync(id, updateDto, userId);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
        public async Task<IActionResult> ChangeStudentStatus(int id, [FromBody] JsonElement statusPayload)
        {
            if (!TryParseStatusPayload(statusPayload, out var status))
            {
                return BadRequest(new
                {
                    message = "Invalid status payload. Use either a raw value like \"Enrolled\" or an object like { \"status\": \"Enrolled\" }."
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _studentService.ChangeStudentStatusAsync(id, status, userId);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        private static bool TryParseStatusPayload(JsonElement payload, out StudentStatus status)
        {
            status = default;

            switch (payload.ValueKind)
            {
                case JsonValueKind.String:
                    return Enum.TryParse(payload.GetString(), true, out status);

                case JsonValueKind.Number:
                    if (payload.TryGetInt32(out var numericStatus) && Enum.IsDefined(typeof(StudentStatus), numericStatus))
                    {
                        status = (StudentStatus)numericStatus;
                        return true;
                    }
                    return false;

                case JsonValueKind.Object:
                    if (payload.TryGetProperty("status", out var statusProperty) ||
                        payload.TryGetProperty("Status", out statusProperty))
                    {
                        return TryParseStatusPayload(statusProperty, out status);
                    }
                    return false;

                default:
                    return false;
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _studentService.DeleteStudentAsync(id, userId);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetStudentDetail(int id)
        {
            var result = await _studentService.GetStudentDetailAsync(id);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        [HttpGet("{id}/registration-summary")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
        public async Task<IActionResult> GetRegistrationSummary(int id)
        {
            var result = await _studentService.GetRegistrationSummaryAsync(id);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        [HttpPost("{id}/cash-payment")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
        public async Task<IActionResult> ProcessCashPayment(int id, [FromBody] CashPaymentDto dto)
        {
            if (dto.StudentId != id)
            {
                dto.StudentId = id;
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _studentService.ProcessCashPaymentAsync(id, dto, userId);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpGet("{id}/payments")]
        public async Task<IActionResult> GetStudentPayments(int id)
        {
            var result = await _stripePaymentService.GetStripePaymentsByStudentIdAsync(id);
            return Ok(result);
        }

        [HttpGet("{id}/cash-payments")]
        public async Task<IActionResult> GetStudentCashPayments(int id)
        {
            var result = await _studentService.GetCashPaymentsByStudentIdAsync(id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpGet("{id}/documents")]
        public async Task<IActionResult> GetStudentDocuments(int id)
        {
            var result = await _fileService.GetStudentDocumentsAsync(id);
            return Ok(result);
        }
    }
}
