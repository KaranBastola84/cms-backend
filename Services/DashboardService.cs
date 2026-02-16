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
                var monthStart = new DateTime(now.Year, now.Month, 1);

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
                    NewToday = students.Count(s => s.CreatedAt.Date == now.Date),
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
                var batchStats = new BatchStatistics
                {
                    Total = batches.Count,
                    Active = batches.Count(b => b.StartDate <= now && (b.EndDate == null || b.EndDate >= now)),
                    Upcoming = batches.Count(b => b.StartDate > now),
                    Completed = batches.Count(b => b.EndDate.HasValue && b.EndDate < now),
                    AverageCapacityUtilization = batches.Any()
                        ? Math.Round((decimal)batches.Average(b => b.Students.Count * 100.0 / b.MaxStudents), 2)
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
                var monthStart = new DateTime(now.Year, now.Month, 1);

                // Get all payment plans with installments
                var paymentPlans = await _context.PaymentPlans
                    .Include(p => p.Installments)
                    .Include(p => p.Student)
                    .ToListAsync();

                // Get all paid installments for revenue calculation
                var allInstallments = paymentPlans.SelectMany(p => p.Installments).ToList();
                var paidInstallments = allInstallments.Where(i => i.Status == InstallmentStatus.Paid).ToList();

                // Revenue metrics
                var revenueMetrics = new RevenueMetrics
                {
                    TotalRevenue = paidInstallments.Sum(i => i.Amount),
                    RevenueToday = paidInstallments
                        .Where(i => i.PaidDate.HasValue && i.PaidDate.Value.Date == now.Date)
                        .Sum(i => i.Amount),
                    RevenueThisWeek = paidInstallments
                        .Where(i => i.PaidDate.HasValue && i.PaidDate >= weekAgo)
                        .Sum(i => i.Amount),
                    RevenueThisMonth = paidInstallments
                        .Where(i => i.PaidDate.HasValue && i.PaidDate >= monthStart)
                        .Sum(i => i.Amount),
                    AverageRevenuePerStudent = paymentPlans.Any()
                        ? Math.Round(paidInstallments.Sum(i => i.Amount) / paymentPlans.Select(p => p.StudentId).Distinct().Count(), 2)
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
                    TotalOutstanding = paymentPlans.Sum(p => p.BalanceAmount),
                    OverdueAmount = overdueInstallments.Sum(i => i.Amount),
                    OverdueInstallments = overdueInstallments.Count,
                    DefaultersCount = defaulters,
                    StudentsPendingFirstPayment = pendingFirstPaymentCount
                };

                // Collection metrics
                var totalExpected = paymentPlans.Sum(p => p.TotalAmount);
                var collectionMetrics = new CollectionMetrics
                {
                    ExpectedRevenue = totalExpected,
                    CollectedRevenue = paymentPlans.Sum(p => p.PaidAmount),
                    CollectionRate = totalExpected > 0
                        ? Math.Round((paymentPlans.Sum(p => p.PaidAmount) / totalExpected) * 100, 2)
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
                    AmountDueNext7Days = upcomingNext7.Sum(i => i.Amount),
                    DueNext30Days = upcomingNext30.Count,
                    AmountDueNext30Days = upcomingNext30.Sum(i => i.Amount)
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

                var courseIds = recentStudents.Where(s => s.CourseId.HasValue).Select(s => s.CourseId.Value).Distinct().ToList();
                var courses = await _context.Courses.Where(c => courseIds.Contains(c.CourseId)).ToDictionaryAsync(c => c.CourseId, c => c);

                var recentStudentDtos = recentStudents.Select(s => new RecentStudentDto
                {
                    StudentId = s.StudentId,
                    Name = s.Name,
                    Email = s.Email,
                    CourseName = s.CourseId.HasValue && courses.ContainsKey(s.CourseId.Value) ? courses[s.CourseId.Value].Name : "N/A",
                    Status = s.Status.ToString(),
                    CreatedAt = s.CreatedAt
                }).ToList();

                // Recent payments
                var recentPayments = await _context.Receipts
                    .Include(r => r.Student)
                    .Where(r => r.PaymentDate.HasValue)
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
                    .Select(g => new RevenueTrendDto
                    {
                        Year = g.Key.Year,
                        Month = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month).Substring(0, 3),
                        Revenue = g.Sum(i => i.Amount),
                        PaymentCount = g.Count()
                    })
                    .OrderBy(r => r.Year).ThenBy(r => r.Month)
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
                    .Select(g => new MonthlyAttendanceDto
                    {
                        Year = g.Key.Year,
                        Month = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month).Substring(0, 3),
                        TotalDays = g.Count(),
                        PresentDays = g.Count(a => a.Status == AttendanceStatus.Present),
                        AttendanceRate = g.Any()
                            ? Math.Round((decimal)g.Count(a => a.Status == AttendanceStatus.Present) / g.Count() * 100, 2)
                            : 0
                    })
                    .OrderBy(m => m.Year).ThenBy(m => m.Month)
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
                    .Select(g => new PaymentCollectionDto
                    {
                        Year = g.Key.Year,
                        Month = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month).Substring(0, 3),
                        Collected = g.Where(i => i.Status == InstallmentStatus.Paid).Sum(i => i.Amount),
                        Outstanding = g.Where(i => i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.Overdue).Sum(i => i.Amount),
                        Expected = g.Sum(i => i.Amount)
                    })
                    .OrderBy(p => p.Year).ThenBy(p => p.Month)
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
                var today = now.Date;
                var weekAgo = now.AddDays(-7);
                var monthStart = new DateTime(now.Year, now.Month, 1);

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
    }
}
