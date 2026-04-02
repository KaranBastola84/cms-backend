using System.Security.Cryptography;
using JWTAuthAPI.Data;
using JWTAuthAPI.Helpers;
using JWTAuthAPI.Models;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace JWTAuthAPI.Services
{
    public class CertificateService : ICertificateService
    {
        private const decimal RequiredAttendancePercentage = 80m;

        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CertificateService> _logger;
        private readonly string _certificatesFolder;

        public CertificateService(
            ApplicationDbContext context,
            IAuditService auditService,
            IEmailService emailService,
            IWebHostEnvironment environment,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CertificateService> logger)
        {
            _context = context;
            _auditService = auditService;
            _emailService = emailService;
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _certificatesFolder = Path.Combine(_environment.ContentRootPath, "Uploads", "Certificates");

            if (!Directory.Exists(_certificatesFolder))
            {
                Directory.CreateDirectory(_certificatesFolder);
            }
        }

        public async Task<ApiResponse<CertificateDto>> RecommendCertificateAsync(CreateCertificateRecommendationDto dto, int trainerId)
        {
            try
            {
                var trainer = await _context.ApplicationUsers
                    .FirstOrDefaultAsync(u => u.Id == trainerId && u.Role == Roles.Trainer);

                if (trainer == null)
                {
                    return ResponseHelper.Error<CertificateDto>("Trainer account not found", 404);
                }

                var student = await _context.Students.FindAsync(dto.StudentId);
                if (student == null)
                {
                    return ResponseHelper.Error<CertificateDto>("Student not found", 404);
                }

                if (!student.BatchId.HasValue)
                {
                    return ResponseHelper.Error<CertificateDto>("Student is not assigned to a batch");
                }

                var batch = await _context.Batches.FindAsync(student.BatchId.Value);
                if (batch == null)
                {
                    return ResponseHelper.Error<CertificateDto>("Student batch not found");
                }

                if (batch.TrainerId != trainerId)
                {
                    return ResponseHelper.Error<CertificateDto>("You can only recommend certificates for students in your assigned batches", 403);
                }

                var normalizedModuleName = NormalizeModuleName(dto.ModuleName);

                var existing = await _context.Certificates
                    .AnyAsync(c => c.StudentId == dto.StudentId
                        && c.ModuleName.ToLower() == normalizedModuleName.ToLower()
                        && c.Status != CertificateStatus.Revoked);

                if (existing)
                {
                    return ResponseHelper.Error<CertificateDto>("An active certificate recommendation already exists for this student and module");
                }

                var eligibility = await BuildEligibilityAsync(student.StudentId);

                var certificate = new Certificate
                {
                    StudentId = student.StudentId,
                    ModuleName = normalizedModuleName,
                    TrainerReportedProgressPercent = dto.ProgressPercent,
                    AttendancePercentage = eligibility.AttendancePercentage,
                    IsPaymentCleared = eligibility.IsPaymentCleared,
                    RecommendationNotes = dto.RecommendationNotes,
                    RecommendedByTrainerId = trainerId,
                    RecommendedAt = DateTime.UtcNow,
                    VerificationToken = GenerateVerificationToken(),
                    Status = CertificateStatus.Recommended,
                    DeliveryMode = CertificateDeliveryMode.Digital,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Certificates.Add(certificate);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    ActionType.CREATE,
                    "Certificate",
                    certificate.CertificateId.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        certificate.StudentId,
                        certificate.ModuleName,
                        certificate.TrainerReportedProgressPercent,
                        certificate.RecommendedByTrainerId
                    }),
                    "Trainer submitted certificate recommendation",
                    trainerId.ToString(),
                    trainer.Email);

                var dtoResult = await MapToDtoAsync(certificate);
                return ResponseHelper.Success(dtoResult, "Certificate recommendation submitted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recommending certificate for student {StudentId}", dto.StudentId);
                return ResponseHelper.Error<CertificateDto>("An error occurred while saving the recommendation");
            }
        }

        public async Task<ApiResponse<List<CertificateDto>>> GetPendingRecommendationsAsync()
        {
            try
            {
                var recommendations = await _context.Certificates
                    .Include(c => c.Student)
                    .Where(c => c.Status == CertificateStatus.Recommended)
                    .OrderByDescending(c => c.RecommendedAt)
                    .ToListAsync();

                var response = new List<CertificateDto>();
                foreach (var recommendation in recommendations)
                {
                    response.Add(await MapToDtoAsync(recommendation));
                }

                return ResponseHelper.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending certificate recommendations");
                return ResponseHelper.Error<List<CertificateDto>>("An error occurred while retrieving recommendations");
            }
        }

        public async Task<ApiResponse<CertificateDto>> GetCertificateByIdAsync(int certificateId)
        {
            try
            {
                var certificate = await _context.Certificates
                    .Include(c => c.Student)
                    .FirstOrDefaultAsync(c => c.CertificateId == certificateId);

                if (certificate == null)
                {
                    return ResponseHelper.NotFound<CertificateDto>("Certificate record not found");
                }

                return ResponseHelper.Success(await MapToDtoAsync(certificate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate {CertificateId}", certificateId);
                return ResponseHelper.Error<CertificateDto>("An error occurred while retrieving certificate");
            }
        }

        public async Task<ApiResponse<CertificateEligibilityDto>> GetCertificateEligibilityAsync(int certificateId)
        {
            try
            {
                var certificate = await _context.Certificates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CertificateId == certificateId);

                if (certificate == null)
                {
                    return ResponseHelper.NotFound<CertificateEligibilityDto>("Certificate recommendation not found");
                }

                var eligibility = await BuildEligibilityAsync(certificate.StudentId);
                return ResponseHelper.Success(eligibility);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking eligibility for certificate {CertificateId}", certificateId);
                return ResponseHelper.Error<CertificateEligibilityDto>("An error occurred while checking eligibility");
            }
        }

        public async Task<ApiResponse<CertificateDto>> IssueCertificateAsync(int certificateId, IssueCertificateDto dto, int adminId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var admin = await _context.ApplicationUsers
                    .FirstOrDefaultAsync(u => u.Id == adminId && u.Role == Roles.Admin);

                if (admin == null)
                {
                    return ResponseHelper.Error<CertificateDto>("Admin account not found", 404);
                }

                var certificate = await _context.Certificates
                    .Include(c => c.Student)
                    .FirstOrDefaultAsync(c => c.CertificateId == certificateId);

                if (certificate == null)
                {
                    return ResponseHelper.NotFound<CertificateDto>("Certificate recommendation not found");
                }

                if (certificate.Status != CertificateStatus.Recommended)
                {
                    return ResponseHelper.Error<CertificateDto>("Only recommended certificates can be issued");
                }

                if (certificate.Student == null)
                {
                    return ResponseHelper.Error<CertificateDto>("Student not found for this recommendation", 404);
                }

                var eligibility = await BuildEligibilityAsync(certificate.StudentId);
                if (!eligibility.IsEligible)
                {
                    return ResponseHelper.Error<CertificateDto>($"Certificate cannot be issued: {string.Join("; ", eligibility.Reasons)}");
                }

                certificate.CertificateNumber = await GenerateCertificateNumberAsync();
                certificate.Status = CertificateStatus.Issued;
                certificate.IssuedByAdminId = adminId;
                certificate.IssuedAt = DateTime.UtcNow;
                certificate.DeliveryMode = dto.DeliveryMode;
                certificate.AdminNotes = dto.AdminNotes;
                certificate.AttendancePercentage = eligibility.AttendancePercentage;
                certificate.IsPaymentCleared = eligibility.IsPaymentCleared;
                certificate.UpdatedAt = DateTime.UtcNow;

                var courseName = await GetCourseNameAsync(certificate.Student.CourseId);
                var verificationUrl = BuildVerificationUrl(certificate.CertificateNumber, certificate.VerificationToken);
                var pdfBytes = GenerateCertificatePdf(certificate.Student, courseName, certificate, verificationUrl);

                var sanitizedNumber = certificate.CertificateNumber.Replace("/", "-").Replace("\\", "-");
                var fileName = $"{sanitizedNumber}.pdf";
                var absolutePath = Path.Combine(_certificatesFolder, fileName);
                await File.WriteAllBytesAsync(absolutePath, pdfBytes);

                certificate.FilePath = absolutePath;

                if (certificate.Student.Status != StudentStatus.Completed)
                {
                    certificate.Student.Status = StudentStatus.Completed;
                    certificate.Student.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Certificate",
                    certificate.CertificateId.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        certificate.CertificateNumber,
                        certificate.IssuedByAdminId,
                        certificate.IssuedAt,
                        certificate.AttendancePercentage,
                        certificate.IsPaymentCleared
                    }),
                    $"Certificate issued for student {certificate.Student.Name}",
                    adminId.ToString(),
                    admin.Email);

                try
                {
                    var downloadUrl = BuildDownloadUrl(certificate.CertificateId);
                    await _emailService.SendCertificateIssuedEmailAsync(
                        certificate.Student.Email,
                        certificate.Student.Name,
                        certificate.CertificateNumber,
                        certificate.ModuleName,
                        certificate.DeliveryMode,
                        downloadUrl);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Certificate email failed for student {StudentId}", certificate.StudentId);
                }

                return ResponseHelper.Success(await MapToDtoAsync(certificate), "Certificate issued successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error issuing certificate {CertificateId}", certificateId);
                return ResponseHelper.Error<CertificateDto>("An error occurred while issuing certificate");
            }
        }

        public async Task<ApiResponse<CertificateDto>> RevokeCertificateAsync(int certificateId, RevokeCertificateDto dto, int adminId)
        {
            try
            {
                var admin = await _context.ApplicationUsers
                    .FirstOrDefaultAsync(u => u.Id == adminId && u.Role == Roles.Admin);

                if (admin == null)
                {
                    return ResponseHelper.Error<CertificateDto>("Admin account not found", 404);
                }

                var certificate = await _context.Certificates
                    .Include(c => c.Student)
                    .FirstOrDefaultAsync(c => c.CertificateId == certificateId);

                if (certificate == null)
                {
                    return ResponseHelper.NotFound<CertificateDto>("Certificate not found");
                }

                if (certificate.Status != CertificateStatus.Issued)
                {
                    return ResponseHelper.Error<CertificateDto>("Only issued certificates can be revoked");
                }

                certificate.Status = CertificateStatus.Revoked;
                certificate.RevocationReason = dto.Reason.Trim();
                certificate.RevokedAt = DateTime.UtcNow;
                certificate.RevokedByAdminId = adminId;
                certificate.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Certificate",
                    certificate.CertificateId.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        certificate.CertificateNumber,
                        certificate.RevocationReason,
                        certificate.RevokedAt
                    }),
                    "Certificate revoked",
                    adminId.ToString(),
                    admin.Email);

                return ResponseHelper.Success(await MapToDtoAsync(certificate), "Certificate revoked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking certificate {CertificateId}", certificateId);
                return ResponseHelper.Error<CertificateDto>("An error occurred while revoking certificate");
            }
        }

        public async Task<ApiResponse<List<CertificateDto>>> GetCertificatesByStudentAsync(int studentId)
        {
            try
            {
                var certificates = await _context.Certificates
                    .Include(c => c.Student)
                    .Where(c => c.StudentId == studentId && c.Status == CertificateStatus.Issued)
                    .OrderByDescending(c => c.IssuedAt)
                    .ToListAsync();

                var result = new List<CertificateDto>();
                foreach (var certificate in certificates)
                {
                    result.Add(await MapToDtoAsync(certificate));
                }

                return ResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificates for student {StudentId}", studentId);
                return ResponseHelper.Error<List<CertificateDto>>("An error occurred while retrieving certificates");
            }
        }

        public async Task<ApiResponse<CertificateFileDto>> GetCertificateFileAsync(int certificateId, int requesterId, string requesterRole)
        {
            try
            {
                var certificate = await _context.Certificates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CertificateId == certificateId);

                if (certificate == null)
                {
                    return ResponseHelper.NotFound<CertificateFileDto>("Certificate not found");
                }

                var role = requesterRole?.Trim() ?? string.Empty;
                var isPrivileged = IsPrivilegedRole(role);
                var isStudent = IsStudentRole(role);

                if (!isPrivileged && !(isStudent && certificate.StudentId == requesterId))
                {
                    return ResponseHelper.Error<CertificateFileDto>("You are not authorized to download this certificate", 403);
                }

                if (certificate.Status != CertificateStatus.Issued)
                {
                    return ResponseHelper.Error<CertificateFileDto>("Only issued certificates can be downloaded", 400);
                }

                if (string.IsNullOrWhiteSpace(certificate.FilePath) || !File.Exists(certificate.FilePath))
                {
                    return ResponseHelper.NotFound<CertificateFileDto>("Certificate file not found");
                }

                return ResponseHelper.Success(new CertificateFileDto
                {
                    AbsolutePath = certificate.FilePath,
                    FileName = $"{certificate.CertificateNumber ?? "certificate"}.pdf",
                    ContentType = "application/pdf"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate file for certificate {CertificateId}", certificateId);
                return ResponseHelper.Error<CertificateFileDto>("An error occurred while preparing certificate download");
            }
        }

        public async Task<ApiResponse<CertificateVerificationDto>> VerifyCertificateAsync(string certificateNumber, string? token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(certificateNumber))
                {
                    return ResponseHelper.Error<CertificateVerificationDto>("Certificate number is required");
                }

                var normalizedCertificateNumber = certificateNumber.Trim();

                var certificate = await _context.Certificates
                    .Include(c => c.Student)
                    .FirstOrDefaultAsync(c => c.CertificateNumber != null && c.CertificateNumber == normalizedCertificateNumber);

                if (certificate == null)
                {
                    return ResponseHelper.NotFound<CertificateVerificationDto>("Certificate not found");
                }

                if (!string.IsNullOrWhiteSpace(token) && !string.Equals(certificate.VerificationToken, token, StringComparison.Ordinal))
                {
                    return ResponseHelper.Success(new CertificateVerificationDto
                    {
                        IsValid = false,
                        CertificateNumber = normalizedCertificateNumber,
                        StudentName = certificate.Student?.Name ?? string.Empty,
                        CourseName = await GetCourseNameAsync(certificate.Student?.CourseId),
                        ModuleName = certificate.ModuleName,
                        IssuedAt = certificate.IssuedAt,
                        Status = certificate.Status,
                        Message = "Invalid verification token"
                    });
                }

                var isValid = certificate.Status == CertificateStatus.Issued;
                var message = certificate.Status switch
                {
                    CertificateStatus.Issued => "Certificate is valid",
                    CertificateStatus.Revoked => $"Certificate has been revoked. Reason: {certificate.RevocationReason}",
                    _ => "Certificate has not been issued yet"
                };

                return ResponseHelper.Success(new CertificateVerificationDto
                {
                    IsValid = isValid,
                    CertificateNumber = certificate.CertificateNumber ?? string.Empty,
                    StudentName = certificate.Student?.Name ?? string.Empty,
                    CourseName = await GetCourseNameAsync(certificate.Student?.CourseId),
                    ModuleName = certificate.ModuleName,
                    IssuedAt = certificate.IssuedAt,
                    Status = certificate.Status,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying certificate {CertificateNumber}", certificateNumber);
                return ResponseHelper.Error<CertificateVerificationDto>("An error occurred while verifying certificate");
            }
        }

        private async Task<CertificateEligibilityDto> BuildEligibilityAsync(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
            {
                return new CertificateEligibilityDto
                {
                    StudentId = studentId,
                    IsEligible = false,
                    Reasons = new List<string> { "Student not found" }
                };
            }

            var attendanceRecords = await _context.Attendances
                .Where(a => a.StudentId == studentId)
                .ToListAsync();

            var attendancePercentage = attendanceRecords.Count > 0
                ? Math.Round((decimal)attendanceRecords.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late) / attendanceRecords.Count * 100m, 2)
                : 0m;

            var paidStripeDirect = await _context.StripePayments
                .Where(p => p.StudentId == studentId && p.Status == PaymentStatus.Paid && !p.InstallmentId.HasValue)
                .SumAsync(p => p.Amount);

            var paidByPlans = await _context.PaymentPlans
                .Where(p => p.StudentId == studentId)
                .SumAsync(p => p.PaidAmount);

            var totalPaid = student.FeesPaid + paidStripeDirect + paidByPlans;
            var isPaymentCleared = student.FeesTotal <= 0 || totalPaid >= student.FeesTotal;
            var isAttendanceEligible = attendancePercentage >= RequiredAttendancePercentage;

            var reasons = new List<string>();
            if (!isAttendanceEligible)
            {
                reasons.Add($"Attendance must be at least {RequiredAttendancePercentage}% (current: {attendancePercentage}%)");
            }

            if (!isPaymentCleared)
            {
                reasons.Add($"Full payment is required (paid {totalPaid:0.##} of {student.FeesTotal:0.##})");
            }

            if (student.Status == StudentStatus.Dropped || student.Status == StudentStatus.Suspended)
            {
                reasons.Add("Student status does not allow certificate issuance");
            }

            return new CertificateEligibilityDto
            {
                StudentId = student.StudentId,
                AttendancePercentage = attendancePercentage,
                IsAttendanceEligible = isAttendanceEligible,
                TotalPaid = totalPaid,
                FeesTotal = student.FeesTotal,
                IsPaymentCleared = isPaymentCleared,
                IsEligible = reasons.Count == 0,
                Reasons = reasons
            };
        }

        private async Task<CertificateDto> MapToDtoAsync(Certificate certificate)
        {
            var student = certificate.Student ?? await _context.Students.FindAsync(certificate.StudentId);
            var courseName = await GetCourseNameAsync(student?.CourseId);

            var recommendedByName = await _context.ApplicationUsers
                .Where(u => u.Id == certificate.RecommendedByTrainerId)
                .Select(u => string.IsNullOrWhiteSpace(u.FirstName) && string.IsNullOrWhiteSpace(u.LastName)
                    ? u.Username
                    : $"{u.FirstName} {u.LastName}".Trim())
                .FirstOrDefaultAsync();

            var issuedByName = certificate.IssuedByAdminId.HasValue
                ? await _context.ApplicationUsers
                    .Where(u => u.Id == certificate.IssuedByAdminId.Value)
                    .Select(u => string.IsNullOrWhiteSpace(u.FirstName) && string.IsNullOrWhiteSpace(u.LastName)
                        ? u.Username
                        : $"{u.FirstName} {u.LastName}".Trim())
                    .FirstOrDefaultAsync()
                : null;

            return new CertificateDto
            {
                CertificateId = certificate.CertificateId,
                CertificateNumber = certificate.CertificateNumber,
                StudentId = certificate.StudentId,
                StudentName = student?.Name ?? string.Empty,
                StudentEmail = student?.Email ?? string.Empty,
                CourseId = student?.CourseId,
                CourseName = courseName,
                ModuleName = certificate.ModuleName,
                TrainerReportedProgressPercent = certificate.TrainerReportedProgressPercent,
                AttendancePercentage = certificate.AttendancePercentage,
                IsPaymentCleared = certificate.IsPaymentCleared,
                Status = certificate.Status,
                DeliveryMode = certificate.DeliveryMode,
                RecommendationNotes = certificate.RecommendationNotes,
                AdminNotes = certificate.AdminNotes,
                RecommendedByTrainerId = certificate.RecommendedByTrainerId,
                RecommendedByTrainerName = recommendedByName,
                RecommendedAt = certificate.RecommendedAt,
                IssuedByAdminId = certificate.IssuedByAdminId,
                IssuedByAdminName = issuedByName,
                IssuedAt = certificate.IssuedAt,
                RevokedAt = certificate.RevokedAt,
                RevocationReason = certificate.RevocationReason,
                DownloadUrl = certificate.Status == CertificateStatus.Issued ? BuildDownloadUrl(certificate.CertificateId) : null,
                VerificationUrl = !string.IsNullOrWhiteSpace(certificate.CertificateNumber)
                    ? BuildVerificationUrl(certificate.CertificateNumber, certificate.VerificationToken)
                    : null
            };
        }

        private async Task<string?> GetCourseNameAsync(int? courseId)
        {
            if (!courseId.HasValue)
            {
                return null;
            }

            return await _context.Courses
                .Where(c => c.CourseId == courseId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
        }

        private async Task<string> GenerateCertificateNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"CMS-CERT-{year}-";

            var lastCertificateNumber = await _context.Certificates
                .Where(c => c.CertificateNumber != null && c.CertificateNumber.StartsWith(prefix))
                .OrderByDescending(c => c.CertificateNumber)
                .Select(c => c.CertificateNumber)
                .FirstOrDefaultAsync();

            var nextSequence = 1;
            if (!string.IsNullOrWhiteSpace(lastCertificateNumber)
                && lastCertificateNumber.Length >= prefix.Length + 4
                && int.TryParse(lastCertificateNumber.Substring(prefix.Length), out var parsed))
            {
                nextSequence = parsed + 1;
            }

            return $"{prefix}{nextSequence:D4}";
        }

        private static string GenerateVerificationToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        }

        private static string NormalizeModuleName(string value)
        {
            var trimmed = value.Trim();
            while (trimmed.Contains("  ", StringComparison.Ordinal))
            {
                trimmed = trimmed.Replace("  ", " ", StringComparison.Ordinal);
            }

            return trimmed;
        }

        private static bool IsPrivilegedRole(string role)
        {
            return role.Equals(Roles.Admin, StringComparison.OrdinalIgnoreCase)
                   || role.Equals(Roles.Staff, StringComparison.OrdinalIgnoreCase)
                   || role.Equals(Roles.Trainer, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStudentRole(string role)
        {
            return role.Equals(Roles.Student, StringComparison.OrdinalIgnoreCase)
                   || role.Equals(Roles.EnrolledStudent, StringComparison.OrdinalIgnoreCase);
        }

        private string BuildDownloadUrl(int certificateId)
        {
            var relative = $"/api/Certificate/{certificateId}/download";
            var baseUrl = BuildBaseUrl();
            return string.IsNullOrWhiteSpace(baseUrl) ? relative : $"{baseUrl}{relative}";
        }

        private string BuildVerificationUrl(string certificateNumber, string token)
        {
            var relative = $"/api/Certificate/verify/{Uri.EscapeDataString(certificateNumber)}?token={Uri.EscapeDataString(token)}";
            var baseUrl = BuildBaseUrl();
            return string.IsNullOrWhiteSpace(baseUrl) ? relative : $"{baseUrl}{relative}";
        }

        private string BuildBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
            {
                return string.Empty;
            }

            return $"{request.Scheme}://{request.Host}";
        }

        private static byte[] GenerateQrCodePng(string content)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }

        private static byte[] GenerateCertificatePdf(Student student, string? courseName, Certificate certificate, string verificationUrl)
        {
            var qrCodeImage = GenerateQrCodePng(verificationUrl);
            var issueDate = certificate.IssuedAt?.ToString("MMMM dd, yyyy") ?? DateTime.UtcNow.ToString("MMMM dd, yyyy");

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontFamily("Times New Roman"));

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().AlignCenter().Text("COFFEE SCHOOL").FontSize(18).SemiBold();
                        column.Item().AlignCenter().Text("Certificate of Completion").FontSize(42).SemiBold().FontColor(Colors.Brown.Darken1);
                        column.Item().AlignCenter().Text("This is to certify that").FontSize(16).Italic();

                        column.Item().AlignCenter().Text(student.Name).FontSize(34).SemiBold().Underline();

                        var courseText = string.IsNullOrWhiteSpace(courseName) ? "the enrolled course" : courseName;
                        column.Item().AlignCenter().Text(
                            $"has successfully completed the module '{certificate.ModuleName}' in {courseText}.").FontSize(16);

                        column.Item().AlignCenter().Text(
                            $"Attendance: {certificate.AttendancePercentage:0.##}%   |   Progress: {certificate.TrainerReportedProgressPercent:0.##}%").FontSize(14);

                        column.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Spacing(6);
                                left.Item().Text($"Certificate Number: {certificate.CertificateNumber}").FontSize(14).SemiBold();
                                left.Item().Text($"Issue Date: {issueDate}").FontSize(13);
                                left.Item().Text("Authorized By: Coffee School Administration").FontSize(13);
                                left.Item().Text("Scan QR to verify authenticity").FontSize(11).FontColor(Colors.Grey.Darken1);
                            });

                            row.ConstantItem(140).Height(140).Image(qrCodeImage);
                        });

                        column.Item().PaddingTop(6).AlignCenter().Text("This certificate is digitally generated and valid without a physical signature.")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    });
                });
            }).GeneratePdf();
        }
    }
}
