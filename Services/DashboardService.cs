using JWTAuthAPI.Data;
using JWTAuthAPI.Helpers;
using JWTAuthAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace JWTAuthAPI.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            ApplicationDbContext context,
            ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<AdminDashboardOverviewDto>> GetAdminOverviewAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var weekAgo = now.AddDays(-7);
                var monthStart = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1), DateTimeKind.Utc);

                // Get student statistics
                var students = await _context.Students.ToListAsync();
                var studentStats = new StudentStatistics
                {
                    Total = students.Count,
                    PendingPayment = students.Count(s => s.Status == StudentStatus.PendingPayment),
                    Enrolled = students.Count(s => s.Status == StudentStatus.Enrolled),
                    Active = students.Count(s => s.Status == StudentStatus.Active),
                    Completed = students.Count(s => s.Status == StudentStatus.Completed),
                    Dropped = students.Count(s => s.Status == StudentStatus.Dropped),
                    Suspended = students.Count(s => s.Status == StudentStatus.Suspended),
                    NewToday = students.Count(s => s.CreatedAt.Date == DateTime.SpecifyKind(now.Date, DateTimeKind.Utc)),
                    NewThisWeek = students.Count(s => s.CreatedAt >= weekAgo),
                    NewThisMonth = students.Count(s => s.CreatedAt >= monthStart)
                };

                // Get course statistics
                var courses = await _context.Courses.ToListAsync();
                var courseStats = new CourseStatistics
                {
                    Total = courses.Count,
                    Active = courses.Count(c => c.IsActive),
                    Inactive = courses.Count(c => !c.IsActive)
                };

                // Get batch statistics
                var batches = await _context.Batches.Include(b => b.Students).ToListAsync();
                var batchesWithCapacity = batches.Where(b => b.MaxStudents > 0).ToList();
                var batchStats = new BatchStatistics
                {
                    Total = batches.Count,
                    Active = batches.Count(b => b.StartDate <= now && (b.EndDate == null || b.EndDate >= now)),
                    Upcoming = batches.Count(b => b.StartDate > now),
                    Completed = batches.Count(b => b.EndDate.HasValue && b.EndDate < now),
                    AverageCapacityUtilization = batchesWithCapacity.Any()
                        ? Math.Round((decimal)batchesWithCapacity.Average(b => b.Students.Count * 100.0 / b.MaxStudents), 2)
                        : 0
                };

                // Get staff/trainer statistics
                var users = await _context.ApplicationUsers.ToListAsync();
                var staffStats = new StaffStatistics
                {
                    TotalUsers = users.Count,
                    TotalStaff = users.Count(u => u.Role == "Staff"),
                    TotalTrainers = users.Count(u => u.Role == "Trainer")
                };

                // Get inquiry statistics
                var inquiries = await _context.Inquiries.ToListAsync();
                var inquiryStats = new InquiryStatistics
                {
                    Total = inquiries.Count,
                    Pending = inquiries.Count(i => i.Status == InquiryStatus.Pending),
                    FollowedUp = inquiries.Count(i => i.Status == InquiryStatus.InProgress),
                    Closed = inquiries.Count(i => i.Status == InquiryStatus.Closed)
                };

                var overview = new AdminDashboardOverviewDto
                {
                    Students = studentStats,
                    Courses = courseStats,
                    Batches = batchStats,
                    Staff = staffStats,
                    Inquiries = inquiryStats
                };

                return ResponseHelper.Success(overview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin overview");
                return ResponseHelper.Error<AdminDashboardOverviewDto>($"An error occurred: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AdminFinancialSummaryDto>> GetFinancialSummaryAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var weekAgo = now.AddDays(-7);
                var monthStart = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1), DateTimeKind.Utc);

                // Get all payment plans with installments
                var paymentPlans = await _context.PaymentPlans
                    .Include(p => p.Installments)
                    .Include(p => p.Student)
                    .ToListAsync();

                // Get all paid installments for revenue calculation
                var allInstallments = paymentPlans.SelectMany(p => p.Installments).ToList();
                var paidInstallments = allInstallments.Where(i => i.Status == InstallmentStatus.Paid).ToList();

                // Revenue metrics
                var todayDate = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
                var todayRevenue = paidInstallments.Where(i => i.PaidDate.HasValue && i.PaidDate.Value.Date == todayDate);
                var weekRevenue = paidInstallments.Where(i => i.PaidDate.HasValue && i.PaidDate >= weekAgo);
                var monthRevenue = paidInstallments.Where(i => i.PaidDate.HasValue && i.PaidDate >= monthStart);
                var distinctStudentCount = paymentPlans.Select(p => p.StudentId).Distinct().Count();

                var revenueMetrics = new RevenueMetrics
                {
                    TotalRevenue = paidInstallments.Any() ? paidInstallments.Sum(i => i.Amount) : 0,
                    RevenueToday = todayRevenue.Any() ? todayRevenue.Sum(i => i.Amount) : 0,
                    RevenueThisWeek = weekRevenue.Any() ? weekRevenue.Sum(i => i.Amount) : 0,
                    RevenueThisMonth = monthRevenue.Any() ? monthRevenue.Sum(i => i.Amount) : 0,
                    AverageRevenuePerStudent = distinctStudentCount > 0 && paidInstallments.Any()
                        ? Math.Round(paidInstallments.Sum(i => i.Amount) / distinctStudentCount, 2)
                        : 0
                };

                // Outstanding metrics
                var overdueInstallments = allInstallments
                    .Where(i => i.Status == InstallmentStatus.Overdue ||
                               (i.Status == InstallmentStatus.Pending && i.DueDate < now))
                    .ToList();

                var defaulters = paymentPlans
                    .Where(p => p.Installments.Any(i =>
                        (i.Status == InstallmentStatus.Overdue || i.Status == InstallmentStatus.Pending) &&
                        i.DueDate < now.AddDays(-7)))
                    .Select(p => p.StudentId)
                    .Distinct()
                    .Count();

                var pendingFirstPaymentCount = await _context.Students
                    .Where(s => s.Status == StudentStatus.PendingPayment)
                    .CountAsync();

                var outstandingMetrics = new OutstandingMetrics
                {
                    TotalOutstanding = paymentPlans.Any() ? paymentPlans.Sum(p => p.BalanceAmount) : 0,
                    OverdueAmount = overdueInstallments.Any() ? overdueInstallments.Sum(i => i.Amount) : 0,
                    OverdueInstallments = overdueInstallments.Count,
                    DefaultersCount = defaulters,
                    StudentsPendingFirstPayment = pendingFirstPaymentCount
                };

                // Collection metrics
                var totalExpected = paymentPlans.Any() ? paymentPlans.Sum(p => p.TotalAmount) : 0;
                var totalCollected = paymentPlans.Any() ? paymentPlans.Sum(p => p.PaidAmount) : 0;
                var collectionMetrics = new CollectionMetrics
                {
                    ExpectedRevenue = totalExpected,
                    CollectedRevenue = totalCollected,
                    CollectionRate = totalExpected > 0
                        ? Math.Round((totalCollected / totalExpected) * 100, 2)
                        : 0
                };

                // Payment plan metrics
                var planMetrics = new PaymentPlanMetrics
                {
                    ActivePlans = paymentPlans.Count(p => p.Status == PaymentPlanStatus.Active),
                    CompletedPlans = paymentPlans.Count(p => p.Status == PaymentPlanStatus.Completed),
                    DefaultedPlans = paymentPlans.Count(p => p.Status == PaymentPlanStatus.Defaulted),
                    SuspendedPlans = paymentPlans.Count(p => p.Status == PaymentPlanStatus.Suspended),
                    CancelledPlans = paymentPlans.Count(p => p.Status == PaymentPlanStatus.Cancelled)
                };

                // Upcoming payments
                var next7Days = now.AddDays(7);
                var next30Days = now.AddDays(30);

                var upcomingNext7 = allInstallments
                    .Where(i => i.Status == InstallmentStatus.Pending && i.DueDate <= next7Days && i.DueDate >= now)
                    .ToList();

                var upcomingNext30 = allInstallments
                    .Where(i => i.Status == InstallmentStatus.Pending && i.DueDate <= next30Days && i.DueDate >= now)
                    .ToList();

                var upcomingPayments = new UpcomingPayments
                {
                    DueNext7Days = upcomingNext7.Count,
                    AmountDueNext7Days = upcomingNext7.Any() ? upcomingNext7.Sum(i => i.Amount) : 0,
                    DueNext30Days = upcomingNext30.Count,
                    AmountDueNext30Days = upcomingNext30.Any() ? upcomingNext30.Sum(i => i.Amount) : 0
                };

                var summary = new AdminFinancialSummaryDto
                {
                    Revenue = revenueMetrics,
                    Outstanding = outstandingMetrics,
                    Collection = collectionMetrics,
                    PaymentPlans = planMetrics,
                    UpcomingPayments = upcomingPayments
                };

                return ResponseHelper.Success(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting financial summary");
                return ResponseHelper.Error<AdminFinancialSummaryDto>($"An error occurred: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AdminRecentActivitiesDto>> GetRecentActivitiesAsync(int limit = 10)
        {
            try
            {
                // Recent students
                var recentStudents = await _context.Students
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(limit)
                    .ToListAsync();

                var courseIds = recentStudents
                    .Where(s => s.CourseId.HasValue && s.CourseId.Value > 0)
                    .Select(s => s.CourseId!.Value)
                    .Distinct()
                    .ToList();

                var courses = courseIds.Any()
                    ? await _context.Courses.Where(c => courseIds.Contains(c.CourseId)).ToDictionaryAsync(c => c.CourseId, c => c)
                    : new Dictionary<int, Course>();

                var recentStudentDtos = recentStudents.Select(s => new RecentStudentDto
                {
                    StudentId = s.StudentId,
                    Name = s.Name,
                    Email = s.Email,
                    CourseName = s.CourseId.HasValue && s.CourseId.Value > 0 && courses.ContainsKey(s.CourseId.Value)
                        ? courses[s.CourseId.Value].Name
                        : "N/A",
                    Status = s.Status.ToString(),
                    CreatedAt = s.CreatedAt
                }).ToList();

                // Recent payments
                var recentPayments = await _context.Receipts
                    .Include(r => r.Student)
                    .Where(r => r.PaymentDate.HasValue && r.Student != null)
                    .OrderByDescending(r => r.PaymentDate)
                    .Take(limit)
                    .Select(r => new RecentPaymentDto
                    {
                        ReceiptId = r.ReceiptId,
                        StudentName = r.Student!.Name,
                        Amount = r.Amount,
                        PaymentMethod = r.PaymentMethod ?? "N/A",
                        PaymentDate = r.PaymentDate!.Value
                    })
                    .ToListAsync();

                // Recent inquiries
                var recentInquiries = await _context.Inquiries
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(limit)
                    .ToListAsync();

                var recentInquiryDtos = recentInquiries.Select(i => new RecentInquiryDto
                {
                    InquiryId = i.Id,
                    Name = i.FullName,
                    Email = i.Email,
                    CourseName = i.CourseInterest ?? "N/A",
                    Status = i.Status.ToString(),
                    CreatedAt = i.CreatedAt
                }).ToList();

                // Upcoming batches
                var now = DateTime.UtcNow;
                var upcomingBatches = await _context.Batches
                    .Include(b => b.Course)
                    .Include(b => b.Students)
                    .Where(b => b.StartDate > now)
                    .OrderBy(b => b.StartDate)
                    .Take(limit)
                    .Select(b => new RecentBatchDto
                    {
                        BatchId = b.BatchId,
                        BatchName = b.Name,
                        CourseName = b.Course!.Name,
                        StartDate = b.StartDate,
                        EnrolledStudents = b.Students.Count,
                        Capacity = b.MaxStudents
                    })
                    .ToListAsync();

                var activities = new AdminRecentActivitiesDto
                {
                    RecentStudents = recentStudentDtos,
                    RecentPayments = recentPayments,
                    RecentInquiries = recentInquiryDtos,
                    UpcomingBatches = upcomingBatches
                };

                return ResponseHelper.Success(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities");
                return ResponseHelper.Error<AdminRecentActivitiesDto>($"An error occurred: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AdminAlertsDto>> GetAlertsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var alerts = new AdminAlertsDto();

                // Payment alerts - overdue installments
                var overdueInstallments = await _context.Installments
                    .Include(i => i.PaymentPlan)
                        .ThenInclude(p => p!.Student)
                    .Where(i => (i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.Overdue)
                               && i.DueDate < now)
                    .ToListAsync();

                var paymentAlerts = overdueInstallments
                    .GroupBy(i => new { i.PaymentPlan!.StudentId, i.PaymentPlan.Student!.Name, i.PaymentPlan.Student.Email })
                    .Select(g => new PaymentAlertDto
                    {
                        StudentId = g.Key.StudentId,
                        StudentName = g.Key.Name,
                        StudentEmail = g.Key.Email,
                        OverdueAmount = g.Sum(i => i.Amount),
                        OverdueDays = (int)(now - g.Min(i => i.DueDate)).TotalDays,
                        OverdueInstallments = g.Count(),
                        Severity = (now - g.Min(i => i.DueDate)).TotalDays > 30 ? "Critical" :
                                  (now - g.Min(i => i.DueDate)).TotalDays > 7 ? "Warning" : "Info"
                    })
                    .OrderByDescending(a => a.OverdueDays)
                    .Take(20)
                    .ToList();

                alerts.PaymentAlerts = paymentAlerts;

                // Attendance alerts - low attendance students
                var attendanceRecords = await _context.Attendances
                    .Include(a => a.Student)
                    .Include(a => a.Batch)
                    .Where(a => a.AttendanceDate >= now.AddDays(-30))
                    .ToListAsync();

                var attendanceAlerts = attendanceRecords
                    .GroupBy(a => new { a.StudentId, a.Student!.Name, BatchName = a.Batch!.Name })
                    .Select(g => new
                    {
                        StudentId = g.Key.StudentId,
                        StudentName = g.Key.Name,
                        BatchName = g.Key.BatchName,
                        TotalDays = g.Count(),
                        PresentDays = g.Count(a => a.Status == AttendanceStatus.Present),
                        ConsecutiveAbsences = CalculateConsecutiveAbsences(g.OrderByDescending(a => a.AttendanceDate).ToList())
                    })
                    .Where(x => x.TotalDays > 0)
                    .Select(x => new AttendanceAlertDto
                    {
                        StudentId = x.StudentId,
                        StudentName = x.StudentName,
                        BatchName = x.BatchName,
                        ConsecutiveAbsences = x.ConsecutiveAbsences,
                        AttendancePercentage = Math.Round((decimal)x.PresentDays / x.TotalDays * 100, 2),
                        Severity = x.ConsecutiveAbsences >= 5 ? "Critical" :
                                  x.ConsecutiveAbsences >= 3 ? "Warning" : "Info"
                    })
                    .Where(x => x.AttendancePercentage < 75 || x.ConsecutiveAbsences >= 3)
                    .OrderByDescending(x => x.ConsecutiveAbsences)
                    .Take(20)
                    .ToList();

                alerts.AttendanceAlerts = attendanceAlerts;

                // Inquiry alerts - pending follow-ups
                var pendingInquiries = await _context.Inquiries
                    .Where(i => i.Status == InquiryStatus.Pending || i.Status == InquiryStatus.InProgress)
                    .OrderBy(i => i.CreatedAt)
                    .Take(20)
                    .Select(i => new InquiryAlertDto
                    {
                        InquiryId = i.Id,
                        Name = i.FullName,
                        Email = i.Email,
                        CourseName = i.CourseInterest ?? "N/A",
                        DaysSinceInquiry = (int)(now - i.CreatedAt).TotalDays,
                        Status = i.Status.ToString()
                    })
                    .ToListAsync();

                alerts.InquiryAlerts = pendingInquiries;

                // Batch alerts - starting soon or low enrollment
                var upcomingBatches = await _context.Batches
                    .Include(b => b.Course)
                    .Include(b => b.Students)
                    .Where(b => b.StartDate > now && b.StartDate <= now.AddDays(14))
                    .ToListAsync();

                var batchAlerts = upcomingBatches
                    .Select(b => new BatchAlertDto
                    {
                        BatchId = b.BatchId,
                        BatchName = b.Name,
                        CourseName = b.Course!.Name,
                        StartDate = b.StartDate,
                        DaysUntilStart = (int)(b.StartDate - now).TotalDays,
                        EnrolledStudents = b.Students.Count,
                        Capacity = b.MaxStudents,
                        Message = b.Students.Count < b.MaxStudents * 0.5
                            ? $"Low enrollment: Only {b.Students.Count}/{b.MaxStudents} students enrolled"
                            : $"Batch starting in {(int)(b.StartDate - now).TotalDays} days"
                    })
                    .OrderBy(b => b.DaysUntilStart)
                    .ToList();

                alerts.BatchAlerts = batchAlerts;

                // Count critical and warning alerts
                alerts.TotalCriticalAlerts =
                    paymentAlerts.Count(a => a.Severity == "Critical") +
                    attendanceAlerts.Count(a => a.Severity == "Critical");

                alerts.TotalWarningAlerts =
                    paymentAlerts.Count(a => a.Severity == "Warning") +
                    attendanceAlerts.Count(a => a.Severity == "Warning") +
                    pendingInquiries.Count(i => i.DaysSinceInquiry > 3) +
                    batchAlerts.Count;

                return ResponseHelper.Success(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alerts");
                return ResponseHelper.Error<AdminAlertsDto>($"An error occurred: {ex.Message}");
            }
        }

        private int CalculateConsecutiveAbsences(List<Attendance> attendances)
        {
            int consecutive = 0;
            foreach (var attendance in attendances)
            {
                if (attendance.Status == AttendanceStatus.Absent)
                {
                    consecutive++;
                }
                else
                {
                    break;
                }
            }
            return consecutive;
        }

        public async Task<ApiResponse<AdminChartsDto>> GetChartsDataAsync(int months = 6)
        {
            try
            {
                var now = DateTime.UtcNow;
                var startDate = now.AddMonths(-months);

                var charts = new AdminChartsDto();

                // Revenue trend - last N months
                var paidInstallments = await _context.Installments
                    .Where(i => i.Status == InstallmentStatus.Paid &&
                               i.PaidDate.HasValue &&
                               i.PaidDate >= startDate)
                    .ToListAsync();

                var revenueTrend = paidInstallments
                    .GroupBy(i => new { i.PaidDate!.Value.Year, i.PaidDate.Value.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        MonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month).Substring(0, 3),
                        Revenue = g.Sum(i => i.Amount),
                        PaymentCount = g.Count()
                    })
                    .OrderBy(r => r.Year).ThenBy(r => r.Month)
                    .Select(r => new RevenueTrendDto
                    {
                        Year = r.Year,
                        Month = r.MonthName,
                        Revenue = r.Revenue,
                        PaymentCount = r.PaymentCount
                    })
                    .ToList();

                charts.RevenueTrend = revenueTrend;

                // Enrollment by course
                var students = await _context.Students
                    .Where(s => s.CourseId.HasValue)
                    .ToListAsync();

                var allCourses = await _context.Courses.ToDictionaryAsync(c => c.CourseId, c => c);

                var paymentPlans = await _context.PaymentPlans
                    .Include(p => p.Course)
                    .ToListAsync();

                var totalStudents = students.Count;
                var enrollmentByCourse = students
                    .Where(s => s.CourseId.HasValue && allCourses.ContainsKey(s.CourseId.Value))
                    .GroupBy(s => allCourses[s.CourseId!.Value].Name)
                    .Select(g => new EnrollmentByCourseDto
                    {
                        CourseName = g.Key,
                        StudentCount = g.Count(),
                        Revenue = paymentPlans
                            .Where(p => p.Course != null && p.Course.Name == g.Key)
                            .Sum(p => p.PaidAmount),
                        Percentage = totalStudents > 0 ? Math.Round((decimal)g.Count() / totalStudents * 100, 2) : 0
                    })
                    .OrderByDescending(e => e.StudentCount)
                    .ToList();

                charts.EnrollmentByCourse = enrollmentByCourse;

                // Student status distribution
                var allStudents = await _context.Students.ToListAsync();
                charts.StudentStatusDistribution = new StudentStatusDistributionDto
                {
                    PendingPayment = allStudents.Count(s => s.Status == StudentStatus.PendingPayment),
                    Enrolled = allStudents.Count(s => s.Status == StudentStatus.Enrolled),
                    Active = allStudents.Count(s => s.Status == StudentStatus.Active),
                    Completed = allStudents.Count(s => s.Status == StudentStatus.Completed),
                    Dropped = allStudents.Count(s => s.Status == StudentStatus.Dropped),
                    Suspended = allStudents.Count(s => s.Status == StudentStatus.Suspended)
                };

                // Monthly attendance - last N months
                var attendanceRecords = await _context.Attendances
                    .Where(a => a.AttendanceDate >= startDate)
                    .ToListAsync();

                var monthlyAttendance = attendanceRecords
                    .GroupBy(a => new { a.AttendanceDate.Year, a.AttendanceDate.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        MonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month).Substring(0, 3),
                        TotalDays = g.Count(),
                        PresentDays = g.Count(a => a.Status == AttendanceStatus.Present),
                        AttendanceRate = g.Any()
                            ? Math.Round((decimal)g.Count(a => a.Status == AttendanceStatus.Present) / g.Count() * 100, 2)
                            : 0
                    })
                    .OrderBy(m => m.Year).ThenBy(m => m.Month)
                    .Select(m => new MonthlyAttendanceDto
                    {
                        Year = m.Year,
                        Month = m.MonthName,
                        TotalDays = m.TotalDays,
                        PresentDays = m.PresentDays,
                        AttendanceRate = m.AttendanceRate
                    })
                    .ToList();

                charts.MonthlyAttendance = monthlyAttendance;

                // Payment collection trend
                var allPaymentPlans = await _context.PaymentPlans
                    .Include(p => p.Installments)
                    .Where(p => p.CreatedAt >= startDate)
                    .ToListAsync();

                var paymentCollection = allPaymentPlans
                    .SelectMany(p => p.Installments.Where(i => i.PaidDate.HasValue || i.Status == InstallmentStatus.Pending))
                    .GroupBy(i => new
                    {
                        Year = i.PaidDate.HasValue ? i.PaidDate.Value.Year : i.CreatedAt.Year,
                        Month = i.PaidDate.HasValue ? i.PaidDate.Value.Month : i.CreatedAt.Month
                    })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        MonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month).Substring(0, 3),
                        Collected = g.Where(i => i.Status == InstallmentStatus.Paid).Any()
                            ? g.Where(i => i.Status == InstallmentStatus.Paid).Sum(i => i.Amount) : 0,
                        Outstanding = g.Where(i => i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.Overdue).Any()
                            ? g.Where(i => i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.Overdue).Sum(i => i.Amount) : 0,
                        Expected = g.Sum(i => i.Amount)
                    })
                    .OrderBy(p => p.Year).ThenBy(p => p.Month)
                    .Select(p => new PaymentCollectionDto
                    {
                        Year = p.Year,
                        Month = p.MonthName,
                        Collected = p.Collected,
                        Outstanding = p.Outstanding,
                        Expected = p.Expected
                    })
                    .ToList();

                charts.PaymentCollection = paymentCollection;

                return ResponseHelper.Success(charts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting charts data");
                return ResponseHelper.Error<AdminChartsDto>($"An error occurred: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AdminAttendanceAnalyticsDto>> GetAttendanceAnalyticsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
                var weekAgo = now.AddDays(-7);
                var monthStart = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1), DateTimeKind.Utc);

                // Today's attendance
                var todayAttendance = await _context.Attendances
                    .Where(a => a.AttendanceDate.Date == today)
                    .ToListAsync();

                // This week's attendance
                var weekAttendance = await _context.Attendances
                    .Where(a => a.AttendanceDate >= weekAgo)
                    .ToListAsync();

                // This month's attendance
                var monthAttendance = await _context.Attendances
                    .Where(a => a.AttendanceDate >= monthStart)
                    .ToListAsync();

                // Overall attendance (last 3 months for performance)
                var overallAttendance = await _context.Attendances
                    .Where(a => a.AttendanceDate >= now.AddMonths(-3))
                    .ToListAsync();

                // Students with low attendance (<75% in last 30 days)
                var last30Days = now.AddDays(-30);
                var recentAttendance = await _context.Attendances
                    .Where(a => a.AttendanceDate >= last30Days)
                    .GroupBy(a => a.StudentId)
                    .Select(g => new
                    {
                        StudentId = g.Key,
                        TotalDays = g.Count(),
                        PresentDays = g.Count(a => a.Status == AttendanceStatus.Present)
                    })
                    .ToListAsync();

                var lowAttendanceCount = recentAttendance
                    .Count(a => a.TotalDays > 0 && (decimal)a.PresentDays / a.TotalDays < 0.75m);

                // Batch-wise attendance for today
                var batchAttendance = await _context.Attendances
                    .Include(a => a.Batch)
                        .ThenInclude(b => b!.Course)
                    .Where(a => a.AttendanceDate.Date == today && a.Batch != null)
                    .GroupBy(a => new { a.BatchId, BatchName = a.Batch!.Name, CourseName = a.Batch.Course!.Name })
                    .Select(g => new
                    {
                        BatchId = g.Key.BatchId,
                        BatchName = g.Key.BatchName,
                        CourseName = g.Key.CourseName,
                        TotalRecords = g.Count(),
                        PresentCount = g.Count(a => a.Status == AttendanceStatus.Present)
                    })
                    .ToListAsync();

                var batchStudentCounts = await _context.Students
                    .Where(s => s.BatchId.HasValue)
                    .GroupBy(s => s.BatchId!.Value)
                    .Select(g => new { BatchId = g.Key, StudentCount = g.Count() })
                    .ToListAsync();

                var batchAttendanceList = batchAttendance
                    .Select(b => new BatchAttendanceDto
                    {
                        BatchId = b.BatchId,
                        BatchName = b.BatchName,
                        CourseName = b.CourseName,
                        TotalStudents = batchStudentCounts.FirstOrDefault(s => s.BatchId == b.BatchId)?.StudentCount ?? 0,
                        PresentToday = b.PresentCount,
                        AttendanceRate = b.TotalRecords > 0
                            ? Math.Round((decimal)b.PresentCount / b.TotalRecords * 100, 2)
                            : 0
                    })
                    .ToList();

                var analytics = new AdminAttendanceAnalyticsDto
                {
                    TodayAttendanceRate = todayAttendance.Any()
                        ? Math.Round((decimal)todayAttendance.Count(a => a.Status == AttendanceStatus.Present) / todayAttendance.Count * 100, 2)
                        : 0,
                    ThisWeekAttendanceRate = weekAttendance.Any()
                        ? Math.Round((decimal)weekAttendance.Count(a => a.Status == AttendanceStatus.Present) / weekAttendance.Count * 100, 2)
                        : 0,
                    ThisMonthAttendanceRate = monthAttendance.Any()
                        ? Math.Round((decimal)monthAttendance.Count(a => a.Status == AttendanceStatus.Present) / monthAttendance.Count * 100, 2)
                        : 0,
                    OverallAttendanceRate = overallAttendance.Any()
                        ? Math.Round((decimal)overallAttendance.Count(a => a.Status == AttendanceStatus.Present) / overallAttendance.Count * 100, 2)
                        : 0,
                    TotalPresentToday = todayAttendance.Count(a => a.Status == AttendanceStatus.Present),
                    TotalAbsentToday = todayAttendance.Count(a => a.Status == AttendanceStatus.Absent),
                    TotalLateToday = todayAttendance.Count(a => a.Status == AttendanceStatus.Late),
                    StudentsWithLowAttendance = lowAttendanceCount,
                    BatchAttendance = batchAttendanceList
                };

                return ResponseHelper.Success(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance analytics");
                return ResponseHelper.Error<AdminAttendanceAnalyticsDto>($"An error occurred: {ex.Message}");
            }
        }

        public async Task<ApiResponse<NotificationResponseDto>> GetNotificationsAsync(int userId, int limit = 50)
        {
            try
            {
                var now = DateTime.UtcNow;
                var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
                var yesterday = today.AddDays(-1);
                var notifications = new List<NotificationDto>();

                // 1. CRITICAL PAYMENT ALERTS - Overdue >30 days
                var criticalPayments = await _context.Installments
                    .Include(i => i.PaymentPlan)
                        .ThenInclude(p => p!.Student)
                    .Where(i => (i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.Overdue)
                               && i.DueDate < now.AddDays(-30))
                    .GroupBy(i => new { i.PaymentPlan!.StudentId, i.PaymentPlan.Student!.Name })
                    .Select(g => new
                    {
                        StudentId = g.Key.StudentId,
                        StudentName = g.Key.Name,
                        OverdueAmount = g.Sum(i => i.Amount),
                        OverdueDays = (int)(now - g.Min(i => i.DueDate)).TotalDays,
                        Count = g.Count()
                    })
                    .Take(10)
                    .ToListAsync();

                foreach (var payment in criticalPayments)
                {
                    notifications.Add(new NotificationDto
                    {
                        Id = $"payment-critical-{payment.StudentId}-{now.Ticks}",
                        Type = "payment",
                        Severity = "critical",
                        Title = "Critical Payment Overdue",
                        Message = $"{payment.StudentName} has {payment.Count} payment(s) overdue by {payment.OverdueDays} days (₹{payment.OverdueAmount:N2})",
                        Timestamp = now.AddDays(-payment.OverdueDays),
                        ActionUrl = $"/students/{payment.StudentId}",
                        RelatedId = payment.StudentId,
                        Metadata = new Dictionary<string, string>
                        {
                            { "amount", payment.OverdueAmount.ToString() },
                            { "days", payment.OverdueDays.ToString() }
                        }
                    });
                }

                // 2. WARNING PAYMENT ALERTS - Overdue 7-30 days
                var warningPayments = await _context.Installments
                    .Include(i => i.PaymentPlan)
                        .ThenInclude(p => p!.Student)
                    .Where(i => (i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.Overdue)
                               && i.DueDate < now.AddDays(-7)
                               && i.DueDate >= now.AddDays(-30))
                    .GroupBy(i => new { i.PaymentPlan!.StudentId, i.PaymentPlan.Student!.Name })
                    .Select(g => new
                    {
                        StudentId = g.Key.StudentId,
                        StudentName = g.Key.Name,
                        OverdueAmount = g.Sum(i => i.Amount),
                        OverdueDays = (int)(now - g.Min(i => i.DueDate)).TotalDays,
                        Count = g.Count()
                    })
                    .Take(10)
                    .ToListAsync();

                foreach (var payment in warningPayments)
                {
                    notifications.Add(new NotificationDto
                    {
                        Id = $"payment-warning-{payment.StudentId}-{now.Ticks}",
                        Type = "payment",
                        Severity = "warning",
                        Title = "Payment Overdue",
                        Message = $"{payment.StudentName} has overdue payment of ₹{payment.OverdueAmount:N2} ({payment.OverdueDays} days)",
                        Timestamp = now.AddDays(-payment.OverdueDays),
                        ActionUrl = $"/students/{payment.StudentId}",
                        RelatedId = payment.StudentId,
                        Metadata = new Dictionary<string, string>
                        {
                            { "amount", payment.OverdueAmount.ToString() },
                            { "days", payment.OverdueDays.ToString() }
                        }
                    });
                }

                // 3. NEW INQUIRIES - Last 24 hours
                var newInquiries = await _context.Inquiries
                    .Where(i => i.CreatedAt >= yesterday)
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                foreach (var inquiry in newInquiries)
                {
                    notifications.Add(new NotificationDto
                    {
                        Id = $"inquiry-new-{inquiry.Id}",
                        Type = "inquiry",
                        Severity = "info",
                        Title = "New Inquiry Received",
                        Message = $"{inquiry.FullName} inquired about {inquiry.CourseInterest ?? "courses"}",
                        Timestamp = inquiry.CreatedAt,
                        ActionUrl = $"/inquiries/{inquiry.Id}",
                        RelatedId = inquiry.Id,
                        Metadata = new Dictionary<string, string>
                        {
                            { "email", inquiry.Email },
                            { "phone", inquiry.PhoneNumber ?? "" }
                        }
                    });
                }

                // 4. PENDING INQUIRIES - Not followed up for >3 days
                var pendingInquiries = await _context.Inquiries
                    .Where(i => (i.Status == InquiryStatus.Pending || i.Status == InquiryStatus.InProgress)
                               && i.CreatedAt < now.AddDays(-3)
                               && i.CreatedAt >= now.AddDays(-30))
                    .OrderBy(i => i.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                foreach (var inquiry in pendingInquiries)
                {
                    var daysSince = (int)(now - inquiry.CreatedAt).TotalDays;
                    notifications.Add(new NotificationDto
                    {
                        Id = $"inquiry-pending-{inquiry.Id}",
                        Type = "inquiry",
                        Severity = "warning",
                        Title = "Inquiry Needs Follow-up",
                        Message = $"{inquiry.FullName}'s inquiry is pending for {daysSince} days",
                        Timestamp = inquiry.CreatedAt,
                        ActionUrl = $"/inquiries/{inquiry.Id}",
                        RelatedId = inquiry.Id,
                        Metadata = new Dictionary<string, string>
                        {
                            { "daysPending", daysSince.ToString() },
                            { "status", inquiry.Status.ToString() }
                        }
                    });
                }

                // 5. ATTENDANCE ALERTS - Critical (5+ consecutive absences or <75% attendance)
                var attendanceRecords = await _context.Attendances
                    .Include(a => a.Student)
                    .Include(a => a.Batch)
                    .Where(a => a.AttendanceDate >= now.AddDays(-30))
                    .ToListAsync();

                var criticalAttendance = attendanceRecords
                    .GroupBy(a => new { a.StudentId, a.Student!.Name, BatchName = a.Batch!.Name })
                    .Select(g => new
                    {
                        StudentId = g.Key.StudentId,
                        StudentName = g.Key.Name,
                        BatchName = g.Key.BatchName,
                        TotalDays = g.Count(),
                        PresentDays = g.Count(a => a.Status == AttendanceStatus.Present),
                        ConsecutiveAbsences = CalculateConsecutiveAbsences(g.OrderByDescending(a => a.AttendanceDate).ToList())
                    })
                    .Where(x => x.TotalDays > 0 && (x.ConsecutiveAbsences >= 5 || (decimal)x.PresentDays / x.TotalDays < 0.75m))
                    .Take(10)
                    .ToList();

                foreach (var attendance in criticalAttendance)
                {
                    var percentage = Math.Round((decimal)attendance.PresentDays / attendance.TotalDays * 100, 1);
                    notifications.Add(new NotificationDto
                    {
                        Id = $"attendance-critical-{attendance.StudentId}",
                        Type = "attendance",
                        Severity = "critical",
                        Title = "Critical Attendance Issue",
                        Message = attendance.ConsecutiveAbsences >= 5
                            ? $"{attendance.StudentName} has {attendance.ConsecutiveAbsences} consecutive absences in {attendance.BatchName}"
                            : $"{attendance.StudentName} has {percentage}% attendance in {attendance.BatchName}",
                        Timestamp = now.AddDays(-1),
                        ActionUrl = $"/students/{attendance.StudentId}",
                        RelatedId = attendance.StudentId,
                        Metadata = new Dictionary<string, string>
                        {
                            { "percentage", percentage.ToString() },
                            { "consecutiveAbsences", attendance.ConsecutiveAbsences.ToString() }
                        }
                    });
                }

                // 6. NEW ADMISSIONS - Today
                var newStudents = await _context.Students
                    .Where(s => s.CreatedAt >= today)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                var courseIds = newStudents.Where(s => s.CourseId.HasValue).Select(s => s.CourseId!.Value).Distinct().ToList();
                var courses = courseIds.Any()
                    ? await _context.Courses.Where(c => courseIds.Contains(c.CourseId)).ToDictionaryAsync(c => c.CourseId, c => c.Name)
                    : new Dictionary<int, string>();

                foreach (var student in newStudents)
                {
                    var courseName = student.CourseId.HasValue && courses.ContainsKey(student.CourseId.Value)
                        ? courses[student.CourseId.Value]
                        : "N/A";

                    notifications.Add(new NotificationDto
                    {
                        Id = $"admission-{student.StudentId}",
                        Type = "admission",
                        Severity = "info",
                        Title = "New Student Admission",
                        Message = $"{student.Name} enrolled in {courseName}",
                        Timestamp = student.CreatedAt,
                        ActionUrl = $"/students/{student.StudentId}",
                        RelatedId = student.StudentId,
                        Metadata = new Dictionary<string, string>
                        {
                            { "course", courseName },
                            { "status", student.Status.ToString() }
                        }
                    });
                }

                // 7. BATCH STARTING SOON - Within 7 days
                var upcomingBatches = await _context.Batches
                    .Include(b => b.Course)
                    .Include(b => b.Students)
                    .Where(b => b.StartDate > now && b.StartDate <= now.AddDays(7))
                    .ToListAsync();

                foreach (var batch in upcomingBatches)
                {
                    var daysUntil = (int)(batch.StartDate - now).TotalDays;
                    var enrollmentPercentage = batch.MaxStudents > 0
                        ? Math.Round((decimal)batch.Students.Count / batch.MaxStudents * 100, 0)
                        : 0;

                    notifications.Add(new NotificationDto
                    {
                        Id = $"batch-starting-{batch.BatchId}",
                        Type = "batch",
                        Severity = enrollmentPercentage < 50 ? "warning" : "info",
                        Title = daysUntil == 0 ? "Batch Starting Today" : $"Batch Starting in {daysUntil} Day(s)",
                        Message = $"{batch.Name} ({batch.Course!.Name}) - {batch.Students.Count}/{batch.MaxStudents} enrolled",
                        Timestamp = batch.StartDate.AddDays(-7),
                        ActionUrl = $"/batches/{batch.BatchId}",
                        RelatedId = batch.BatchId,
                        Metadata = new Dictionary<string, string>
                        {
                            { "enrollment", batch.Students.Count.ToString() },
                            { "capacity", batch.MaxStudents.ToString() },
                            { "daysUntil", daysUntil.ToString() }
                        }
                    });
                }

                // 8. RECENT PAYMENTS - Today (Info notifications)
                var recentPayments = await _context.Receipts
                    .Include(r => r.Student)
                    .Where(r => r.PaymentDate.HasValue && r.PaymentDate >= today)
                    .OrderByDescending(r => r.PaymentDate)
                    .Take(10)
                    .ToListAsync();

                foreach (var payment in recentPayments)
                {
                    notifications.Add(new NotificationDto
                    {
                        Id = $"payment-received-{payment.ReceiptId}",
                        Type = "payment_received",
                        Severity = "info",
                        Title = "Payment Received",
                        Message = $"₹{payment.Amount:N2} received from {payment.Student?.Name ?? "Student"}",
                        Timestamp = payment.PaymentDate!.Value,
                        ActionUrl = $"/students/{payment.StudentId}",
                        RelatedId = payment.StudentId,
                        Metadata = new Dictionary<string, string>
                        {
                            { "amount", payment.Amount.ToString() },
                            { "method", payment.PaymentMethod ?? "N/A" }
                        }
                    });
                }

                // 9. NEW ORDERS - Pending orders (last 24 hours)
                var newOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Pending && o.OrderDate >= yesterday)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(20)
                    .ToListAsync();

                foreach (var order in newOrders)
                {
                    notifications.Add(new NotificationDto
                    {
                        Id = $"order-new-{order.Id}",
                        Type = "order",
                        Severity = "info",
                        Title = "New Order Received",
                        Message = $"Order {order.OrderNumber} from {order.CustomerName} - ${order.TotalAmount:N2}",
                        Timestamp = order.OrderDate,
                        ActionUrl = $"/orders/{order.Id}",
                        RelatedId = order.Id,
                        Metadata = new Dictionary<string, string>
                        {
                            { "amount", order.TotalAmount.ToString() },
                            { "customer", order.CustomerEmail }
                        }
                    });
                }

                // 10. LOW STOCK ALERTS - Critical (stock below threshold)
                var lowStockProducts = await _context.Products
                    .Where(p => p.IsActive && p.StockQuantity <= p.LowStockThreshold)
                    .OrderBy(p => p.StockQuantity)
                    .Take(10)
                    .ToListAsync();

                foreach (var product in lowStockProducts)
                {
                    var severity = product.StockQuantity == 0 ? "critical" : "warning";
                    var title = product.StockQuantity == 0 ? "Out of Stock" : "Low Stock Alert";

                    notifications.Add(new NotificationDto
                    {
                        Id = $"stock-low-{product.Id}",
                        Type = "low_stock",
                        Severity = severity,
                        Title = title,
                        Message = $"{product.Name} - Stock: {product.StockQuantity} (Threshold: {product.LowStockThreshold})",
                        Timestamp = product.UpdatedAt,
                        ActionUrl = $"/products/{product.Id}",
                        RelatedId = product.Id,
                        Metadata = new Dictionary<string, string>
                        {
                            { "stock", product.StockQuantity.ToString() },
                            { "threshold", product.LowStockThreshold.ToString() }
                        }
                    });
                }

                // 11. NEW PRODUCT REVIEWS - Pending approval
                var pendingReviews = await _context.ProductReviews
                    .Include(r => r.Product)
                    .Where(r => !r.IsApproved && r.CreatedAt >= now.AddDays(-7))
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                foreach (var review in pendingReviews)
                {
                    notifications.Add(new NotificationDto
                    {
                        Id = $"review-pending-{review.Id}",
                        Type = "review",
                        Severity = "info",
                        Title = "New Product Review",
                        Message = $"{review.CustomerName} reviewed {review.Product.Name} ({review.Rating}★)",
                        Timestamp = review.CreatedAt,
                        ActionUrl = $"/reviews/{review.Id}",
                        RelatedId = review.Id,
                        Metadata = new Dictionary<string, string>
                        {
                            { "rating", review.Rating.ToString() },
                            { "product", review.Product.Name }
                        }
                    });
                }

                // Sort all notifications by timestamp (newest first) and take limit
                var sortedNotifications = notifications
                    .OrderByDescending(n => n.Timestamp)
                    .Take(limit)
                    .ToList();

                // Get read notifications for this user
                var notificationKeys = sortedNotifications.Select(n => n.Id).ToList();
                var readNotifications = await _context.UserNotificationReads
                    .Where(r => r.UserId == userId && notificationKeys.Contains(r.NotificationKey))
                    .Select(r => r.NotificationKey)
                    .ToListAsync();

                // Mark notifications as read/unread
                foreach (var notification in sortedNotifications)
                {
                    notification.IsRead = readNotifications.Contains(notification.Id);
                }

                // Calculate counts
                var response = new NotificationResponseDto
                {
                    Notifications = sortedNotifications,
                    UnreadCount = sortedNotifications.Count(n => !n.IsRead),
                    CriticalCount = sortedNotifications.Count(n => n.Severity == "critical" && !n.IsRead),
                    WarningCount = sortedNotifications.Count(n => n.Severity == "warning" && !n.IsRead)
                };

                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return ResponseHelper.Error<NotificationResponseDto>($"An error occurred: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> MarkNotificationAsReadAsync(int userId, string notificationKey)
        {
            try
            {
                // Check if already marked as read
                var existing = await _context.UserNotificationReads
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.NotificationKey == notificationKey);

                if (existing != null)
                {
                    return ResponseHelper.Success(true, "Notification already marked as read");
                }

                // Parse notification key to get type and related ID
                var parts = notificationKey.Split('-');
                if (parts.Length < 3)
                {
                    return ResponseHelper.Error<bool>("Invalid notification key format");
                }

                var notificationType = parts[0];
                if (!int.TryParse(parts[2], out int relatedId))
                {
                    return ResponseHelper.Error<bool>("Invalid notification key format");
                }

                // Create new read record
                var readRecord = new UserNotificationRead
                {
                    UserId = userId,
                    NotificationType = notificationType,
                    RelatedId = relatedId,
                    NotificationKey = notificationKey,
                    ReadAt = DateTime.UtcNow
                };

                _context.UserNotificationReads.Add(readRecord);
                await _context.SaveChangesAsync();

                return ResponseHelper.Success(true, "Notification marked as read");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return ResponseHelper.Error<bool>($"An error occurred: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> MarkAllNotificationsAsReadAsync(int userId)
        {
            try
            {
                var now = DateTime.UtcNow;
                var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
                var yesterday = today.AddDays(-1);

                // Get all current notification keys (same logic as GetNotificationsAsync but just IDs)
                var notificationKeys = new List<string>();

                // 1. Critical payments
                var criticalPayments = await _context.Installments
                    .Include(i => i.PaymentPlan)
                    .Where(i => (i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.Overdue)
                               && i.DueDate < now.AddDays(-30))
                    .GroupBy(i => i.PaymentPlan!.StudentId)
                    .Select(g => g.Key)
                    .Take(10)
                    .ToListAsync();

                notificationKeys.AddRange(criticalPayments.Select(id => $"payment-critical-{id}"));

                // 2. Warning payments
                var warningPayments = await _context.Installments
                    .Include(i => i.PaymentPlan)
                    .Where(i => (i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.Overdue)
                               && i.DueDate < now.AddDays(-7)
                               && i.DueDate >= now.AddDays(-30))
                    .GroupBy(i => i.PaymentPlan!.StudentId)
                    .Select(g => g.Key)
                    .Take(10)
                    .ToListAsync();

                notificationKeys.AddRange(warningPayments.Select(id => $"payment-warning-{id}"));

                // 3. New inquiries
                var newInquiries = await _context.Inquiries
                    .Where(i => i.CreatedAt >= yesterday)
                    .Select(i => i.Id)
                    .Take(20)
                    .ToListAsync();

                notificationKeys.AddRange(newInquiries.Select(id => $"inquiry-new-{id}"));

                // 4. Pending inquiries
                var pendingInquiries = await _context.Inquiries
                    .Where(i => (i.Status == InquiryStatus.Pending || i.Status == InquiryStatus.InProgress)
                               && i.CreatedAt < now.AddDays(-3)
                               && i.CreatedAt >= now.AddDays(-30))
                    .Select(i => i.Id)
                    .Take(10)
                    .ToListAsync();

                notificationKeys.AddRange(pendingInquiries.Select(id => $"inquiry-pending-{id}"));

                // 5. Critical attendance (simplified - just get student IDs with issues)
                var attendanceStudents = await _context.Attendances
                    .Where(a => a.AttendanceDate >= now.AddDays(-30))
                    .GroupBy(a => a.StudentId)
                    .Select(g => new
                    {
                        StudentId = g.Key,
                        TotalDays = g.Count(),
                        PresentDays = g.Count(a => a.Status == AttendanceStatus.Present)
                    })
                    .Where(x => x.TotalDays > 0 && (decimal)x.PresentDays / x.TotalDays < 0.75m)
                    .Select(x => x.StudentId)
                    .Take(10)
                    .ToListAsync();

                notificationKeys.AddRange(attendanceStudents.Select(id => $"attendance-critical-{id}"));

                // 6. New admissions
                var newStudents = await _context.Students
                    .Where(s => s.CreatedAt >= today)
                    .Select(s => s.StudentId)
                    .Take(20)
                    .ToListAsync();

                notificationKeys.AddRange(newStudents.Select(id => $"admission-{id}"));

                // 7. Batch starting soon
                var upcomingBatches = await _context.Batches
                    .Where(b => b.StartDate > now && b.StartDate <= now.AddDays(7))
                    .Select(b => b.BatchId)
                    .ToListAsync();

                notificationKeys.AddRange(upcomingBatches.Select(id => $"batch-starting-{id}"));

                // 8. Recent payments
                var recentPayments = await _context.Receipts
                    .Where(r => r.PaymentDate.HasValue && r.PaymentDate >= today)
                    .Select(r => r.ReceiptId)
                    .Take(10)
                    .ToListAsync();

                notificationKeys.AddRange(recentPayments.Select(id => $"payment-received-{id}"));

                // Get already read notifications
                var alreadyRead = await _context.UserNotificationReads
                    .Where(r => r.UserId == userId && notificationKeys.Contains(r.NotificationKey))
                    .Select(r => r.NotificationKey)
                    .ToListAsync();

                // Mark unread ones as read
                var toMarkAsRead = notificationKeys.Except(alreadyRead).Distinct().ToList();

                foreach (var key in toMarkAsRead)
                {
                    var parts = key.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int relatedId))
                    {
                        _context.UserNotificationReads.Add(new UserNotificationRead
                        {
                            UserId = userId,
                            NotificationType = parts[0],
                            RelatedId = relatedId,
                            NotificationKey = key,
                            ReadAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return ResponseHelper.Success(true, $"Marked {toMarkAsRead.Count} notifications as read");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return ResponseHelper.Error<bool>($"An error occurred: {ex.Message}");
            }
        }
    }
}
