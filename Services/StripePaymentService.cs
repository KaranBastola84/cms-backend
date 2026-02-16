using JWTAuthAPI.Data;
using JWTAuthAPI.Helpers;
using JWTAuthAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace JWTAuthAPI.Services
{
    public class StripePaymentService : IStripePaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IPaymentPlanService _paymentPlanService;
        private readonly ILogger<StripePaymentService> _logger;
        private readonly StripeSettings _stripeSettings;

        public StripePaymentService(
            ApplicationDbContext context,
            IAuditService auditService,
            IPaymentPlanService paymentPlanService,
            ILogger<StripePaymentService> logger,
            IOptions<StripeSettings> stripeSettings)
        {
            _context = context;
            _auditService = auditService;
            _paymentPlanService = paymentPlanService;
            _logger = logger;
            _stripeSettings = stripeSettings.Value;

            // Set Stripe API key
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
        }

        public async Task<ApiResponse<StripePaymentResponseDto>> CreatePaymentIntentAsync(CreateStripePaymentDto dto, string createdBy)
        {
            try
            {
                // Validate student
                var student = await _context.Students.FindAsync(dto.StudentId);
                if (student == null)
                {
                    return ResponseHelper.Error<StripePaymentResponseDto>("Student not found");
                }

                // Validate installment
                var installment = await _context.Installments
                    .Include(i => i.PaymentPlan)
                    .FirstOrDefaultAsync(i => i.InstallmentId == dto.InstallmentId);

                if (installment == null)
                {
                    return ResponseHelper.Error<StripePaymentResponseDto>("Installment not found");
                }

                if (installment.Status == InstallmentStatus.Paid)
                {
                    return ResponseHelper.Error<StripePaymentResponseDto>("Installment already paid");
                }

                // Create Stripe Payment Intent
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(dto.Amount * 100), // Convert to cents
                    Currency = dto.Currency.ToLower(),
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "student_id", dto.StudentId.ToString() },
                        { "installment_id", dto.InstallmentId.ToString() },
                        { "student_name", student.Name },
                        { "student_email", student.Email },
                        { "created_by", createdBy }
                    }
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                // Save to database
                var stripePayment = new StripePayment
                {
                    PaymentIntentId = paymentIntent.Id,
                    StudentId = dto.StudentId,
                    InstallmentId = dto.InstallmentId,
                    Amount = dto.Amount,
                    Currency = dto.Currency,
                    Status = Models.PaymentStatus.Pending,
                    ClientSecret = paymentIntent.ClientSecret,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(dto.Metadata),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.StripePayments.Add(stripePayment);
                await _context.SaveChangesAsync();

                // Update installment with payment intent ID
                installment.StripePaymentIntentId = paymentIntent.Id;
                installment.Status = InstallmentStatus.Pending;
                await _context.SaveChangesAsync();

                // Log
                await _auditService.LogAsync(
                    ActionType.CREATE,
                    "StripePayment",
                    stripePayment.StripePaymentId.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(stripePayment),
                    $"Stripe payment intent created for student {student.Name} by {createdBy}"
                );

                var response = MapToResponseDto(stripePayment);
                return ResponseHelper.Success(response);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error creating payment intent");
                return ResponseHelper.Error<StripePaymentResponseDto>($"Stripe error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent");
                return ResponseHelper.Error<StripePaymentResponseDto>("An error occurred while creating payment intent");
            }
        }

        public async Task<ApiResponse<StripePaymentResponseDto>> GetPaymentByIdAsync(int stripePaymentId)
        {
            try
            {
                var payment = await _context.StripePayments
                    .Include(p => p.Student)
                    .Include(p => p.Installment)
                    .FirstOrDefaultAsync(p => p.StripePaymentId == stripePaymentId);

                if (payment == null)
                {
                    return ResponseHelper.Error<StripePaymentResponseDto>("Payment not found");
                }

                var response = MapToResponseDto(payment);
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment");
                return ResponseHelper.Error<StripePaymentResponseDto>("An error occurred");
            }
        }

        public async Task<ApiResponse<StripePaymentResponseDto>> GetStripePaymentByIdAsync(int paymentId)
        {
            return await GetPaymentByIdAsync(paymentId);
        }

        public async Task<ApiResponse<List<StripePaymentResponseDto>>> GetStripePaymentsByStudentIdAsync(int studentId)
        {
            try
            {
                var payments = await _context.StripePayments
                    .Include(p => p.Student)
                    .Include(p => p.Installment)
                    .Where(p => p.StudentId == studentId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var response = payments.Select(MapToResponseDto).ToList();
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student payments");
                return ResponseHelper.Error<List<StripePaymentResponseDto>>("An error occurred while retrieving payments");
            }
        }

        public async Task<ApiResponse<StripePaymentResponseDto>> GetPaymentByIntentIdAsync(string paymentIntentId)
        {
            try
            {
                var payment = await _context.StripePayments
                    .Include(p => p.Student)
                    .Include(p => p.Installment)
                    .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId);

                if (payment == null)
                {
                    return ResponseHelper.Error<StripePaymentResponseDto>("Payment not found");
                }

                var response = MapToResponseDto(payment);
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment");
                return ResponseHelper.Error<StripePaymentResponseDto>("An error occurred");
            }
        }

        public async Task<ApiResponse<string>> HandleWebhookAsync(Event stripeEvent)
        {
            try
            {
                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent != null)
                    {
                        await HandlePaymentSuccessAsync(paymentIntent);
                    }
                }
                else if (stripeEvent.Type == "payment_intent.payment_failed")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent != null)
                    {
                        await HandlePaymentFailedAsync(paymentIntent);
                    }
                }

                return ResponseHelper.Success("Webhook handled successfully");
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe webhook error");
                return ResponseHelper.Error<string>($"Webhook error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling webhook");
                return ResponseHelper.Error<string>("An error occurred while handling webhook");
            }
        }

        public async Task<ApiResponse<StripePaymentResponseDto>> ConfirmPaymentAsync(int paymentId)
        {
            try
            {
                var payment = await _context.StripePayments
                    .Include(p => p.Student)
                    .Include(p => p.Installment)
                    .FirstOrDefaultAsync(p => p.StripePaymentId == paymentId);

                if (payment == null)
                {
                    return ResponseHelper.Error<StripePaymentResponseDto>("Payment not found");
                }

                // Get payment intent from Stripe
                var service = new PaymentIntentService();
                var paymentIntent = await service.GetAsync(payment.PaymentIntentId);

                // Update payment status
                payment.Status = paymentIntent.Status == "succeeded" ? Models.PaymentStatus.Paid : Models.PaymentStatus.Failed;
                payment.PaymentMethod = paymentIntent.PaymentMethodTypes?.FirstOrDefault();
                payment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                if (payment.Status == Models.PaymentStatus.Paid && payment.InstallmentId.HasValue)
                {
                    // Mark installment as paid
                    var payDto = new PayInstallmentDto
                    {
                        InstallmentId = payment.InstallmentId.Value,
                        Amount = payment.Amount,
                        PaymentMethod = "Stripe",
                        Remarks = $"Paid via Stripe - Payment Intent: {payment.PaymentIntentId}"
                    };

                    await _paymentPlanService.PayInstallmentAsync(payment.InstallmentId.Value, payDto, "System");
                }

                var response = MapToResponseDto(payment);
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment");
                return ResponseHelper.Error<StripePaymentResponseDto>("An error occurred while confirming payment");
            }
        }

        // Private helper methods
        private async Task HandlePaymentSuccessAsync(PaymentIntent paymentIntent)
        {
            var payment = await _context.StripePayments
                .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntent.Id);

            if (payment != null)
            {
                payment.Status = Models.PaymentStatus.Paid;
                payment.PaymentMethod = paymentIntent.PaymentMethodTypes?.FirstOrDefault();
                payment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Mark installment as paid
                if (payment.InstallmentId.HasValue)
                {
                    var payDto = new PayInstallmentDto
                    {
                        InstallmentId = payment.InstallmentId.Value,
                        Amount = payment.Amount,
                        PaymentMethod = "Stripe",
                        Remarks = $"Paid via Stripe - Webhook confirmed"
                    };

                    await _paymentPlanService.PayInstallmentAsync(payment.InstallmentId.Value, payDto, "System");
                }

                // Log success
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "StripePayment",
                    payment.StripePaymentId.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(payment),
                    $"Payment succeeded via webhook"
                );
            }
        }

        private async Task HandlePaymentFailedAsync(PaymentIntent paymentIntent)
        {
            var payment = await _context.StripePayments
                .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntent.Id);

            if (payment != null)
            {
                payment.Status = Models.PaymentStatus.Failed;
                payment.ErrorMessage = paymentIntent.LastPaymentError?.Message;
                payment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log failure
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "StripePayment",
                    payment.StripePaymentId.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(payment),
                    $"Payment failed: {payment.ErrorMessage}"
                );
            }
        }

        private StripePaymentResponseDto MapToResponseDto(StripePayment payment)
        {
            return new StripePaymentResponseDto
            {
                StripePaymentId = payment.StripePaymentId,
                PaymentIntentId = payment.PaymentIntentId,
                ClientSecret = payment.ClientSecret ?? string.Empty,
                StudentId = payment.StudentId,
                InstallmentId = payment.InstallmentId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Status = payment.Status,
                StatusText = payment.Status.ToString(),
                PaymentMethod = payment.PaymentMethod,
                ErrorMessage = payment.ErrorMessage,
                CreatedAt = payment.CreatedAt
            };
        }
    }
}
