using JWTAuthAPI.Models;
using JWTAuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JWTAuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentPlanController : ControllerBase
    {
        private readonly IPaymentPlanService _paymentPlanService;

        public PaymentPlanController(IPaymentPlanService paymentPlanService)
        {
            _paymentPlanService = paymentPlanService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> CreatePaymentPlan([FromBody] CreatePaymentPlanDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _paymentPlanService.CreatePaymentPlanAsync(dto, userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPaymentPlan(int id)
        {
            var result = await _paymentPlanService.GetPaymentPlanByIdAsync(id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetPaymentPlansByStudent(int studentId)
        {
            var result = await _paymentPlanService.GetPaymentPlansByStudentIdAsync(studentId);
            return Ok(result);
        }

        [HttpGet("course/{courseId}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetPaymentPlansByCourse(int courseId)
        {
            var result = await _paymentPlanService.GetPaymentPlansByCourseIdAsync(courseId);
            return Ok(result);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdatePaymentPlanStatus(int id, [FromBody] UpdatePaymentPlanStatusDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _paymentPlanService.UpdatePaymentPlanStatusAsync(id, dto.Status, userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("installments/{installmentId}")]
        public async Task<IActionResult> GetInstallment(int installmentId)
        {
            var result = await _paymentPlanService.GetInstallmentByIdAsync(installmentId);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpPost("installments/{installmentId}/pay")]
        public async Task<IActionResult> PayInstallment(int installmentId, [FromBody] PayInstallmentDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _paymentPlanService.PayInstallmentAsync(installmentId, dto, userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("installments/overdue")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetOverdueInstallments([FromQuery] int? days)
        {
            var result = await _paymentPlanService.GetOverdueInstallmentsAsync(days);
            return Ok(result);
        }

        [HttpGet("installments/upcoming")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetUpcomingInstallments([FromQuery] int days = 7)
        {
            var result = await _paymentPlanService.GetUpcomingInstallmentsAsync(days);
            return Ok(result);
        }
    }
}
