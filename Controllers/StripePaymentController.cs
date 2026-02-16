using JWTAuthAPI.Models;
using JWTAuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.Security.Claims;

namespace JWTAuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StripePaymentController : ControllerBase
    {
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripePaymentController> _logger;

        public StripePaymentController(
            IStripePaymentService stripePaymentService,
            IConfiguration configuration,
            ILogger<StripePaymentController> logger)
        {
            _stripePaymentService = stripePaymentService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("create-payment-intent")]
        [Authorize]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] CreateStripePaymentDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _stripePaymentService.CreatePaymentIntentAsync(dto, userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{paymentId}")]
        [Authorize]
        public async Task<IActionResult> GetPayment(int paymentId)
        {
            var result = await _stripePaymentService.GetStripePaymentByIdAsync(paymentId);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpGet("student/{studentId}")]
        [Authorize]
        public async Task<IActionResult> GetPaymentsByStudent(int studentId)
        {
            var result = await _stripePaymentService.GetStripePaymentsByStudentIdAsync(studentId);
            return Ok(result);
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeSignature = Request.Headers["Stripe-Signature"];
                var webhookSecret = _configuration["StripeSettings:WebhookSecret"];

                if (string.IsNullOrWhiteSpace(webhookSecret))
                {
                    _logger.LogWarning("Webhook secret is not configured. Processing webhook without validation.");
                    // For development, process the event without validation
                    var eventWithoutValidation = EventUtility.ParseEvent(json);
                    var result = await _stripePaymentService.HandleWebhookAsync(eventWithoutValidation);
                    return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
                }

                var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
                var validatedResult = await _stripePaymentService.HandleWebhookAsync(stripeEvent);
                return validatedResult.IsSuccess ? Ok() : BadRequest(validatedResult.ErrorMessage);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe webhook error");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook processing error");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{paymentId}/confirm")]
        [Authorize]
        public async Task<IActionResult> ConfirmPayment(int paymentId)
        {
            var result = await _stripePaymentService.ConfirmPaymentAsync(paymentId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
