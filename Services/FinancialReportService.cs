using JWTAuthAPI.Data;
using JWTAuthAPI.Helpers;
using JWTAuthAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace JWTAuthAPI.Services
{
    public class FinancialReportService : IFinancialReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FinancialReportService> _logger;

        public FinancialReportService(
            ApplicationDbContext context,
            ILogger<FinancialReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<FinancialSummaryDto>> GetFinancialSummaryAsync()
        {
            try
            {
                var paymentPlans = await _context.PaymentPlans
                    .Include(p => p.Student)
                    .Include(p => p.Course)
                    .Include(p => p.Installments)
                    .ToListAsync();

                var summary = new FinancialSummaryDto
                {
                    TotalRevenue = paymentPlans.Sum(p => p.PaidAmount),
                    OutstandingAmount = paymentPlans.Sum(p => p.BalanceAmount),
                    TotalExpectedRevenue = paymentPlans.Sum(p => p.TotalAmount),
                    ActivePaymentPlans = paymentPlans.Count(p => p.Status == PaymentPlanStatus.Active),
                    CompletedPaymentPlans = paymentPlans.Count(p => p.Status == PaymentPlanStatus.Completed),
                    DefaultedPaymentPlans = paymentPlans.Count(p => p.Status == PaymentPlanStatus.Defaulted),
                    SuspendedPaymentPlans = paymentPlans.Count(p => p.Status == PaymentPlanStatus.Suspended),
                    TotalPaidInstallments = paymentPlans.Sum(p => p.Installments.Count(i => i.Status == InstallmentStatus.Paid)),
                    TotalPendingInstallments = paymentPlans.Sum(p => p.Installments.Count(i => i.Status == InstallmentStatus.Pending)),
                    TotalOverdueInstallments = paymentPlans.Sum(p => p.Installments.Count(i => i.Status == InstallmentStatus.Overdue)),
                    CollectionRate = CalculateCollectionRate(paymentPlans)
                };

                return ResponseHelper.Success(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting financial summary");
                return ResponseHelper.Error<FinancialSummaryDto>("An error occurred");
            }
        }

        public async Task<ApiResponse<List<OutstandingPaymentDto>>> GetOutstandingPaymentsAsync()
        {
            try
            {
                var outstandingPayments = await _context.PaymentPlans
                    .Include(p => p.Student)
                    .Include(p => p.Course)
                    .Include(p => p.Installments)
                    .Where(p => p.BalanceAmount > 0 && p.Status != PaymentPlanStatus.Completed)
                    .Select(p => new OutstandingPaymentDto
                    {
                        PaymentPlanId = p.PaymentPlanId,
                        StudentId = p.StudentId,
                        StudentName = p.Student!.Name,
                        StudentEmail = p.Student.Email ?? string.Empty,
                        CourseId = p.CourseId,
                        CourseName = p.Course!.Name,
                        TotalAmount = p.TotalAmount,
                        PaidAmount = p.PaidAmount,
                        BalanceAmount = p.BalanceAmount,
                        OutstandingAmount = p.BalanceAmount,
                        Status = p.Status.ToString(),
                        PendingInstallments = p.Installments.Count(i => i.Status == InstallmentStatus.Pending),
                        OverdueInstallments = p.Installments.Count(i => i.Status == InstallmentStatus.Overdue),
                        OverdueInstallmentsCount = p.Installments.Count(i => i.Status == InstallmentStatus.Overdue),
                        OverdueAmount = p.Installments.Where(i => i.Status == InstallmentStatus.Overdue).Sum(i => i.Amount),
                        NextDueDate = p.Installments
                            .Where(i => i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.Overdue)
                            .OrderBy(i => i.DueDate)
                            .Select(i => (DateTime?)i.DueDate)
                            .FirstOrDefault()
                    })
                    .OrderByDescending(o => o.BalanceAmount)
                    .ToListAsync();

                return ResponseHelper.Success(outstandingPayments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outstanding payments");
                return ResponseHelper.Error<List<OutstandingPaymentDto>>("An error occurred");
            }
        }

        public async Task<ApiResponse<List<DefaulterStudentDto>>> GetDefaultersAsync(int overdueThresholdDays = 7)
        {
            try
            {
                var thresholdDate = DateTime.UtcNow.AddDays(-overdueThresholdDays);

                var defaulters = await _context.PaymentPlans
                    .Include(p => p.Student)
                    .Include(p => p.Course)
                    .Include(p => p.Installments)
                    .Where(p => p.Installments.Any(i => 
                        i.Status == InstallmentStatus.Overdue && 
                        i.DueDate < thresholdDate))
                    .Select(p => new DefaulterStudentDto
                    {
                        StudentId = p.StudentId,
                        StudentName = p.Student!.Name,
                        StudentEmail = p.Student.Email ?? string.Empty,
                        StudentPhone = p.Student.Phone,
                        CourseId = p.CourseId ?? 0,
                        CourseName = p.Course!.Name,
                        PaymentPlanId = p.PaymentPlanId,
                        TotalOverdueAmount = p.Installments
                            .Where(i => i.Status == InstallmentStatus.Overdue)
                            .Sum(i => i.Amount),
                        OverdueInstallments = p.Installments.Count(i => i.Status == InstallmentStatus.Overdue),
                        OldestOverdueDays = p.Installments
                            .Where(i => i.Status == InstallmentStatus.Overdue)
                            .Min(i => (int)(DateTime.UtcNow - i.DueDate).TotalDays),
                        LastPaymentDate = p.Installments
                            .Where(i => i.Status == InstallmentStatus.Paid && i.PaidDate.HasValue)
                            .OrderByDescending(i => i.PaidDate)
                            .Select(i => i.PaidDate)
                            .FirstOrDefault()
                    })
                    .OrderByDescending(d => d.TotalOverdueAmount)
                    .ToListAsync();

                return ResponseHelper.Success(defaulters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting defaulters");
                return ResponseHelper.Error<List<DefaulterStudentDto>>("An error occurred");
            }
        }

        public async Task<ApiResponse<RevenueReportDto>> GetRevenueReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Ensure dates are in UTC
                startDate = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
                endDate = DateTime.SpecifyKind(endDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

                var paidInstallments = await _context.Installments
                    .Include(i => i.PaymentPlan)
                        .ThenInclude(p => p!.Course)
                    .Include(i => i.PaymentPlan)
                        .ThenInclude(p => p!.Student)
                    .Where(i => i.Status == InstallmentStatus.Paid && 
                               i.PaidDate.HasValue && 
                               i.PaidDate >= startDate && 
                               i.PaidDate <= endDate)
                    .ToListAsync();

                var courseRevenues = paidInstallments
                    .Where(i => i.PaymentPlan != null && i.PaymentPlan.Course != null)
                    .GroupBy(i => new { i.PaymentPlan!.CourseId, CourseName = i.PaymentPlan!.Course!.Name })
                    .Select(g => new CourseRevenueBreakdownDto
                    {
                        CourseId = g.Key.CourseId ?? 0,
                        CourseName = g.Key.CourseName,
                        TotalRevenue = g.Sum(i => i.Amount),
                        PaymentCount = g.Count(),
                        StudentCount = g.Where(i => i.PaymentPlan != null).Select(i => i.PaymentPlan!.StudentId).Distinct().Count()
                    })
                    .OrderByDescending(c => c.TotalRevenue)
                    .ToList();

                var report = new RevenueReportDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalRevenue = paidInstallments.Sum(i => i.Amount),
                    TotalPayments = paidInstallments.Count,
                    UniquePayingStudents = paidInstallments.Where(i => i.PaymentPlan != null).Select(i => i.PaymentPlan!.StudentId).Distinct().Count(),
                    CourseRevenues = courseRevenues,
                    AveragePaymentAmount = paidInstallments.Any() ? paidInstallments.Average(i => i.Amount) : 0,
                    GeneratedAt = DateTime.UtcNow
                };

                return ResponseHelper.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating revenue report");
                return ResponseHelper.Error<RevenueReportDto>("An error occurred");
            }
        }

        public async Task<ApiResponse<CourseRevenueDto>> GetCourseRevenueAsync(int courseId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                {
                    return ResponseHelper.Error<CourseRevenueDto>("Course not found");
                }

                var query = _context.PaymentPlans
                    .Include(p => p.Installments)
                    .Include(p => p.Student)
                    .Where(p => p.CourseId == courseId);

                if (startDate.HasValue)
                {
                    var utcStartDate = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                    query = query.Where(p => p.CreatedAt >= utcStartDate);
                }

                if (endDate.HasValue)
                {
                    var utcEndDate = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(p => p.CreatedAt <= utcEndDate);
                }

                var paymentPlans = await query.ToListAsync();

                var courseRevenue = new CourseRevenueDto
                {
                    CourseId = courseId,
                    CourseName = course.Name,
                    TotalEnrolled = paymentPlans.Count,
                    TotalExpectedRevenue = paymentPlans.Sum(p => p.TotalAmount),
                    TotalCollectedRevenue = paymentPlans.Sum(p => p.PaidAmount),
                    TotalOutstanding = paymentPlans.Sum(p => p.BalanceAmount),
                    ActivePlans = paymentPlans.Count(p => p.Status == PaymentPlanStatus.Active),
                    CompletedPlans = paymentPlans.Count(p => p.Status == PaymentPlanStatus.Completed),
                    DefaultedPlans = paymentPlans.Count(p => p.Status == PaymentPlanStatus.Defaulted),
                    CollectionRate = CalculateCollectionRate(paymentPlans),
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedAt = DateTime.UtcNow
                };

                return ResponseHelper.Success(courseRevenue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating course revenue report");
                return ResponseHelper.Error<CourseRevenueDto>("An error occurred");
            }
        }

        private decimal CalculateCollectionRate(List<PaymentPlan> paymentPlans)
        {
            if (!paymentPlans.Any())
                return 0;

            var totalExpected = paymentPlans.Sum(p => p.TotalAmount);
            if (totalExpected == 0)
                return 0;

            var totalCollected = paymentPlans.Sum(p => p.PaidAmount);
            return Math.Round((totalCollected / totalExpected) * 100, 2);
        }
    }
}
