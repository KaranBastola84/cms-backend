using JWTAuthAPI.Data;
using JWTAuthAPI.Helpers;
using JWTAuthAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace JWTAuthAPI.Services
{
    public class PaymentPlanService : IPaymentPlanService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IReceiptService _receiptService;
        private readonly IStudentService _studentService;
        private readonly ILogger<PaymentPlanService> _logger;

        public PaymentPlanService(
            ApplicationDbContext context,
            IAuditService auditService,
            IReceiptService receiptService,
            IStudentService studentService,
            ILogger<PaymentPlanService> logger)
        {
            _context = context;
            _auditService = auditService;
            _receiptService = receiptService;
            _studentService = studentService;
            _logger = logger;
        }

        public async Task<ApiResponse<PaymentPlanResponseDto>> CreatePaymentPlanAsync(CreatePaymentPlanDto dto, string createdBy)
        {
            try
            {
                // Validate student exists
                var student = await _context.Students.FindAsync(dto.StudentId);
                if (student == null)
                {
                    return ResponseHelper.Error<PaymentPlanResponseDto>("Student not found");
                }

                // Validate course if provided
                Course? course = null;
                if (dto.CourseId.HasValue)
                {
                    course = await _context.Courses.FindAsync(dto.CourseId.Value);
                    if (course == null)
                    {
                        return ResponseHelper.Error<PaymentPlanResponseDto>("Course not found");
                    }
                }

                // Create payment plan
                var paymentPlan = new PaymentPlan
                {
                    StudentId = dto.StudentId,
                    CourseId = dto.CourseId,
                    TotalAmount = dto.TotalAmount,
                    BalanceAmount = dto.TotalAmount,
                    NumberOfInstallments = dto.NumberOfInstallments,
                    Status = PaymentPlanStatus.Active,
                    Description = dto.Description,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.PaymentPlans.Add(paymentPlan);
                await _context.SaveChangesAsync();

                // Create installments
                var installmentAmount = Math.Round(dto.TotalAmount / dto.NumberOfInstallments, 2);
                var remainingAmount = dto.TotalAmount;
                var firstDueDate = dto.FirstInstallmentDueDate ?? DateTime.UtcNow.AddDays(30);

                for (int i = 1; i <= dto.NumberOfInstallments; i++)
                {
                    var amount = i == dto.NumberOfInstallments ? remainingAmount : installmentAmount;
                    
                    var installment = new Installment
                    {
                        PaymentPlanId = paymentPlan.PaymentPlanId,
                        InstallmentNumber = i,
                        Amount = amount,
                        DueDate = firstDueDate.AddMonths(i - 1),
                        Status = InstallmentStatus.Pending,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Installments.Add(installment);
                    remainingAmount -= amount;
                }

                await _context.SaveChangesAsync();

                // Load navigation properties
                await _context.Entry(paymentPlan).Reference(p => p.Student).LoadAsync();
                await _context.Entry(paymentPlan).Reference(p => p.Course).LoadAsync();
                await _context.Entry(paymentPlan).Collection(p => p.Installments).LoadAsync();

                // Log
                await _auditService.LogAsync(
                    ActionType.CREATE,
                    "PaymentPlan",
                    paymentPlan.PaymentPlanId.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(paymentPlan),
                    $"Payment plan created for student {student.Name}"
                );

                var response = MapToResponseDto(paymentPlan);
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment plan");
                return ResponseHelper.Error<PaymentPlanResponseDto>("An error occurred while creating payment plan");
            }
        }

        public async Task<ApiResponse<PaymentPlanResponseDto>> GetPaymentPlanByIdAsync(int paymentPlanId)
        {
            try
            {
                var paymentPlan = await _context.PaymentPlans
                    .Include(p => p.Student)
                    .Include(p => p.Course)
                    .Include(p => p.Installments)
                    .FirstOrDefaultAsync(p => p.PaymentPlanId == paymentPlanId);

                if (paymentPlan == null)
                {
                    return ResponseHelper.Error<PaymentPlanResponseDto>("Payment plan not found");
                }

                var response = MapToResponseDto(paymentPlan);
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment plan");
                return ResponseHelper.Error<PaymentPlanResponseDto>("An error occurred while retrieving payment plan");
            }
        }

        public async Task<ApiResponse<List<PaymentPlanResponseDto>>> GetPaymentPlansByStudentIdAsync(int studentId)
        {
            try
            {
                var paymentPlans = await _context.PaymentPlans
                    .Include(p => p.Student)
                    .Include(p => p.Course)
                    .Include(p => p.Installments)
                    .Where(p => p.StudentId == studentId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var response = paymentPlans.Select(MapToResponseDto).ToList();
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student payment plans");
                return ResponseHelper.Error<List<PaymentPlanResponseDto>>("An error occurred while retrieving payment plans");
            }
        }

        public async Task<ApiResponse<List<PaymentPlanResponseDto>>> GetPaymentPlansByCourseIdAsync(int courseId)
        {
            try
            {
                var paymentPlans = await _context.PaymentPlans
                    .Include(p => p.Student)
                    .Include(p => p.Course)
                    .Include(p => p.Installments)
                    .Where(p => p.CourseId == courseId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var response = paymentPlans.Select(MapToResponseDto).ToList();
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course payment plans");
                return ResponseHelper.Error<List<PaymentPlanResponseDto>>("An error occurred while retrieving payment plans");
            }
        }

        public async Task<ApiResponse<PaymentPlanResponseDto>> UpdatePaymentPlanStatusAsync(int paymentPlanId, PaymentPlanStatus status, string updatedBy)
        {
            try
            {
                var paymentPlan = await _context.PaymentPlans
                    .Include(p => p.Student)
                    .Include(p => p.Course)
                    .Include(p => p.Installments)
                    .FirstOrDefaultAsync(p => p.PaymentPlanId == paymentPlanId);

                if (paymentPlan == null)
                {
                    return ResponseHelper.Error<PaymentPlanResponseDto>("Payment plan not found");
                }

                var oldData = System.Text.Json.JsonSerializer.Serialize(paymentPlan);

                paymentPlan.Status = status;
                paymentPlan.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "PaymentPlan",
                    paymentPlan.PaymentPlanId.ToString(),
                    oldData,
                    System.Text.Json.JsonSerializer.Serialize(paymentPlan),
                    $"Payment plan status updated to {status}"
                );

                var response = MapToResponseDto(paymentPlan);
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment plan status");
                return ResponseHelper.Error<PaymentPlanResponseDto>("An error occurred while updating payment plan status");
            }
        }

        public async Task<ApiResponse<InstallmentResponseDto>> GetInstallmentByIdAsync(int installmentId)
        {
            try
            {
                var installment = await _context.Installments
                    .Include(i => i.PaymentPlan)
                        .ThenInclude(p => p!.Student)
                    .Include(i => i.PaymentPlan)
                        .ThenInclude(p => p!.Course)
                    .FirstOrDefaultAsync(i => i.InstallmentId == installmentId);

                if (installment == null)
                {
                    return ResponseHelper.Error<InstallmentResponseDto>("Installment not found");
                }

                var response = MapInstallmentToDto(installment);
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting installment");
                return ResponseHelper.Error<InstallmentResponseDto>("An error occurred while retrieving installment");
            }
        }

        public async Task<ApiResponse<PaymentPlanResponseDto>> UpdatePaymentPlanAsync(int paymentPlanId, UpdatePaymentPlanDto dto, string updatedBy)
        {
            try
            {
                var paymentPlan = await _context.PaymentPlans
                    .Include(p => p.Student)
                    .Include(p => p.Course)
                    .Include(p => p.Installments)
                    .FirstOrDefaultAsync(p => p.PaymentPlanId == paymentPlanId);

                if (paymentPlan == null)
                {
                    return ResponseHelper.Error<PaymentPlanResponseDto>("Payment plan not found");
                }

                var oldData = System.Text.Json.JsonSerializer.Serialize(paymentPlan);

                if (dto.Status.HasValue)
                    paymentPlan.Status = dto.Status.Value;

                if (!string.IsNullOrWhiteSpace(dto.Description))
                    paymentPlan.Description = dto.Description;

                paymentPlan.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "PaymentPlan",
                    paymentPlan.PaymentPlanId.ToString(),
                    oldData,
                    System.Text.Json.JsonSerializer.Serialize(paymentPlan),
                    $"Payment plan updated"
                );

                var response = MapToResponseDto(paymentPlan);
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment plan");
                return ResponseHelper.Error<PaymentPlanResponseDto>("An error occurred while updating payment plan");
            }
        }

        public async Task<ApiResponse<string>> DeletePaymentPlanAsync(int paymentPlanId, string deletedBy)
        {
            try
            {
                var paymentPlan = await _context.PaymentPlans
                    .Include(p => p.Installments)
                    .FirstOrDefaultAsync(p => p.PaymentPlanId == paymentPlanId);

                if (paymentPlan == null)
                {
                    return ResponseHelper.Error<string>("Payment plan not found");
                }

                // Check if any payment has been made
                if (paymentPlan.PaidAmount > 0)
                {
                    return ResponseHelper.Error<string>("Cannot delete payment plan with payments made");
                }

                var oldData = System.Text.Json.JsonSerializer.Serialize(paymentPlan);

                _context.PaymentPlans.Remove(paymentPlan);
                await _context.SaveChangesAsync();

                // Log
                await _auditService.LogAsync(
                    ActionType.DELETE,
                    "PaymentPlan",
                    paymentPlanId.ToString(),
                    oldData,
                    null,
                    $"Payment plan deleted"
                );

                return ResponseHelper.Success("Payment plan deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment plan");
                return ResponseHelper.Error<string>("An error occurred while deleting payment plan");
            }
        }

        public async Task<ApiResponse<InstallmentResponseDto>> PayInstallmentAsync(int installmentId, PayInstallmentDto dto, string processedBy)
        {
            try
            {
                var installment = await _context.Installments
                    .Include(i => i.PaymentPlan)
                        .ThenInclude(p => p!.Student)
                    .FirstOrDefaultAsync(i => i.InstallmentId == installmentId);

                if (installment == null)
                {
                    return ResponseHelper.Error<InstallmentResponseDto>("Installment not found");
                }

                if (installment.Status == InstallmentStatus.Paid)
                {
                    return ResponseHelper.Error<InstallmentResponseDto>("Installment already paid");
                }

                // Update installment
                installment.Status = InstallmentStatus.Paid;
                installment.PaidDate = DateTime.UtcNow;
                installment.Remarks = dto.Remarks;
                installment.UpdatedAt = DateTime.UtcNow;

                // Update payment plan
                var paymentPlan = installment.PaymentPlan;
                if (paymentPlan != null)
                {
                    paymentPlan.PaidAmount += dto.Amount;
                    paymentPlan.BalanceAmount -= dto.Amount;

                    if (paymentPlan.BalanceAmount <= 0)
                    {
                        paymentPlan.Status = PaymentPlanStatus.Completed;
                    }
                    paymentPlan.UpdatedAt = DateTime.UtcNow;
                }

                // Generate receipt
                var receiptDto = new CreateReceiptDto
                {
                    StudentId = installment.PaymentPlan!.StudentId,
                    Amount = dto.Amount,
                    ReceiptType = ReceiptType.TuitionFee,
                    Description = $"Payment for Installment #{installment.InstallmentNumber}",
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = dto.PaymentMethod
                };

                var receiptResult = await _receiptService.GenerateReceiptAsync(receiptDto, processedBy);
                if (receiptResult.IsSuccess && receiptResult.Result != null)
                {
                    installment.ReceiptId = receiptResult.Result.ReceiptId;
                }

                await _context.SaveChangesAsync();

                // Complete admission after first payment
                if (installment.PaymentPlan?.Student?.Status == StudentStatus.PendingPayment)
                {
                    await _studentService.CompleteAdmissionAsync(installment.PaymentPlan.StudentId, processedBy);
                    _logger.LogInformation("Student {StudentId} admission completed after first payment", installment.PaymentPlan.StudentId);
                }

                // Log
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Installment",
                    installment.InstallmentId.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(installment),
                    $"Installment paid via {dto.PaymentMethod}"
                );

                var response = MapInstallmentToDto(installment);
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error paying installment");
                return ResponseHelper.Error<InstallmentResponseDto>("An error occurred while processing payment");
            }
        }

        public async Task<ApiResponse<List<InstallmentResponseDto>>> GetOverdueInstallmentsAsync(int? days)
        {
            try
            {
                var query = _context.Installments
                    .Include(i => i.PaymentPlan)
                    .Where(i => i.Status == InstallmentStatus.Pending && i.DueDate < DateTime.UtcNow);

                if (days.HasValue)
                {
                    var thresholdDate = DateTime.UtcNow.AddDays(-days.Value);
                    query = query.Where(i => i.DueDate < thresholdDate);
                }

                var overdueInstallments = await query
                    .OrderBy(i => i.DueDate)
                    .ToListAsync();

                // Update status to overdue
                foreach (var installment in overdueInstallments)
                {
                    installment.Status = InstallmentStatus.Overdue;
                }
                await _context.SaveChangesAsync();

                var response = overdueInstallments.Select(MapInstallmentToDto).ToList();
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue installments");
                return ResponseHelper.Error<List<InstallmentResponseDto>>("An error occurred");
            }
        }

        public async Task<ApiResponse<List<InstallmentResponseDto>>> GetUpcomingInstallmentsAsync(int days = 7)
        {
            try
            {
                var upcomingDate = DateTime.UtcNow.AddDays(days);
                var upcomingInstallments = await _context.Installments
                    .Include(i => i.PaymentPlan)
                    .Where(i => i.Status == InstallmentStatus.Pending 
                             && i.DueDate >= DateTime.UtcNow 
                             && i.DueDate <= upcomingDate)
                    .OrderBy(i => i.DueDate)
                    .ToListAsync();

                var response = upcomingInstallments.Select(MapInstallmentToDto).ToList();
                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming installments");
                return ResponseHelper.Error<List<InstallmentResponseDto>>("An error occurred");
            }
        }

        // Helper methods
        private PaymentPlanResponseDto MapToResponseDto(PaymentPlan plan)
        {
            return new PaymentPlanResponseDto
            {
                PaymentPlanId = plan.PaymentPlanId,
                StudentId = plan.StudentId,
                StudentName = plan.Student?.Name ?? "Unknown",
                CourseId = plan.CourseId,
                CourseName = plan.Course?.Name,
                TotalAmount = plan.TotalAmount,
                PaidAmount = plan.PaidAmount,
                BalanceAmount = plan.BalanceAmount,
                NumberOfInstallments = plan.NumberOfInstallments,
                Status = plan.Status,
                StatusText = plan.Status.ToString(),
                Description = plan.Description,
                CreatedAt = plan.CreatedAt,
                UpdatedAt = plan.UpdatedAt,
                CreatedBy = plan.CreatedBy,
                Installments = plan.Installments?.Select(MapInstallmentToDto).ToList() ?? new List<InstallmentResponseDto>()
            };
        }

        private InstallmentResponseDto MapInstallmentToDto(Installment installment)
        {
            return new InstallmentResponseDto
            {
                InstallmentId = installment.InstallmentId,
                PaymentPlanId = installment.PaymentPlanId,
                InstallmentNumber = installment.InstallmentNumber,
                Amount = installment.Amount,
                DueDate = installment.DueDate,
                PaidDate = installment.PaidDate,
                Status = installment.Status,
                StatusText = installment.Status.ToString(),
                ReceiptId = installment.ReceiptId,
                StripePaymentIntentId = installment.StripePaymentIntentId,
                Remarks = installment.Remarks
            };
        }
    }
}
