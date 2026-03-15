using JWTAuthAPI.Data;
using JWTAuthAPI.Helpers;
using JWTAuthAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JWTAuthAPI.Services
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Student> _passwordHasher;
        private readonly IAuditService _auditService;
        private readonly IEmailService _emailService;
        private readonly ILogger<StudentService> _logger;

        public StudentService(
            ApplicationDbContext context,
            IPasswordHasher<Student> passwordHasher,
            IAuditService auditService,
            IEmailService emailService,
            ILogger<StudentService> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _auditService = auditService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<ApiResponse<StudentDto>> CreateStudentAsync(CreateStudentDto createDto, string createdBy)
        {
            // Use transaction to prevent race conditions
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var normalizedEmail = NormalizeEmail(createDto.Email);

                // Check if email already exists
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.Email.ToLower() == normalizedEmail);

                if (existingStudent != null)
                {
                    return ResponseHelper.Error<StudentDto>($"A student with email '{createDto.Email.Trim()}' already exists");
                }

                // Validate batch capacity (with row lock to prevent race condition)
                var capacityCheck = await ValidateBatchCapacityAsync(createDto.BatchId, null);
                if (!capacityCheck.IsSuccess)
                {
                    return ResponseHelper.Error<StudentDto>(capacityCheck.ErrorMessage.FirstOrDefault() ?? "Batch capacity validation failed");
                }

                var student = new Student
                {
                    Name = createDto.Name,
                    Email = createDto.Email.Trim(),
                    Phone = createDto.Phone,
                    CourseId = createDto.CourseId,
                    BatchId = createDto.BatchId,
                    Address = createDto.Address,
                    EmergencyContact = createDto.EmergencyContact,
                    Notes = createDto.Notes,
                    Status = StudentStatus.PendingPayment,
                    AdmissionDate = null,  // Will be set when payment is made
                    FeesTotal = createDto.FeesTotal,
                    FeesPaid = createDto.FeesPaid ?? 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Generate a random password for the student portal
                var tempPassword = GenerateTemporaryPassword();
                student.PasswordHash = _passwordHasher.HashPassword(student, tempPassword);

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // No email at registration — credentials will be sent after payment

                // Log the action
                await _auditService.LogAsync(
                    ActionType.CREATE,
                    "Student",
                    student.StudentId.ToString(),
                    null,
                    $"Created new student (pending payment): {student.Name} ({student.Email})",
                    $"Awaiting payment confirmation.",
                    createdBy
                );

                await transaction.CommitAsync();

                var studentDto = MapToDto(student);
                return ResponseHelper.Success(studentDto, "Student registered successfully. Please complete payment to confirm admission.");
            }
            catch (DbUpdateException ex) when (IsDuplicateEmailDbError(ex))
            {
                await transaction.RollbackAsync();
                return ResponseHelper.Error<StudentDto>($"A student with email '{createDto.Email.Trim()}' already exists");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating student");
                return ResponseHelper.Error<StudentDto>("An error occurred while creating the student");
            }
        }

        // Complete admission after first payment
        public async Task<ApiResponse<string>> CompleteAdmissionAsync(int studentId, string completedBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var student = await _context.Students.FindAsync(studentId);

                if (student == null)
                {
                    return ResponseHelper.Error<string>("Student not found");
                }

                if (student.Status != StudentStatus.PendingPayment)
                {
                    return ResponseHelper.Success("Student admission already completed");
                }

                var oldData = System.Text.Json.JsonSerializer.Serialize(student);

                // Update student status to Enrolled and set admission date
                student.Status = StudentStatus.Enrolled;
                student.AdmissionDate = DateTime.UtcNow;
                student.UpdatedAt = DateTime.UtcNow;

                // Generate fresh credentials for the student
                var tempPassword = GenerateTemporaryPassword();
                student.PasswordHash = _passwordHasher.HashPassword(student, tempPassword);

                await _context.SaveChangesAsync();

                // Send single combined email with admission confirmation + credentials
                try
                {
                    await _emailService.SendAdmissionConfirmationEmailAsync(student.Email, student, tempPassword);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send admission confirmation email to {Email}", student.Email);
                }

                // Log the action
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Student",
                    student.StudentId.ToString(),
                    oldData,
                    System.Text.Json.JsonSerializer.Serialize(student),
                    $"Student admission completed after payment: {student.Name}",
                    completedBy
                );

                await transaction.CommitAsync();

                return ResponseHelper.Success($"Admission completed successfully for {student.Name}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error completing student admission");
                return ResponseHelper.Error<string>("An error occurred while completing admission");
            }
        }

        public async Task<ApiResponse<StudentDto>> GetStudentByIdAsync(int studentId)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);

                if (student == null)
                {
                    return ResponseHelper.Error<StudentDto>("Student not found");
                }

                var studentDto = MapToDto(student);
                return ResponseHelper.Success(studentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student");
                return ResponseHelper.Error<StudentDto>("An error occurred while retrieving the student");
            }
        }

        public async Task<ApiResponse<StudentDetailDto>> GetStudentDetailAsync(int studentId)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);

                if (student == null)
                {
                    return ResponseHelper.Error<StudentDetailDto>("Student not found");
                }

                // Fetch course and batch names
                string? courseName = null;
                decimal courseFee = 0;
                if (student.CourseId.HasValue)
                {
                    var course = await _context.Courses.FindAsync(student.CourseId.Value);
                    courseName = course?.Name;
                    courseFee = course?.Fees ?? 0;
                }

                string? batchName = null;
                if (student.BatchId.HasValue)
                {
                    var batch = await _context.Batches.FindAsync(student.BatchId.Value);
                    batchName = batch?.Name;
                }

                // Fetch Stripe payments for this student
                var payments = await _context.StripePayments
                    .Where(p => p.StudentId == studentId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                // Fetch documents for this student
                var documents = await _context.StudentDocuments
                    .Where(d => d.StudentId == studentId)
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();

                var paidPayments = payments.Where(p => p.Status == PaymentStatus.Paid).ToList();

                var detail = new StudentDetailDto
                {
                    StudentId = student.StudentId,
                    Name = student.Name,
                    Email = student.Email,
                    Phone = student.Phone,
                    CourseId = student.CourseId,
                    CourseName = courseName,
                    BatchId = student.BatchId,
                    BatchName = batchName,
                    Status = student.Status.ToString(),
                    Address = student.Address,
                    EmergencyContact = student.EmergencyContact,
                    Notes = student.Notes,
                    CreatedAt = student.CreatedAt,
                    UpdatedAt = student.UpdatedAt,
                    AdmissionDate = student.AdmissionDate,
                    FeesPaid = student.FeesPaid,
                    FeesTotal = student.FeesTotal,
                    CourseFee = courseFee,
                    FeesRemaining = student.FeesTotal - student.FeesPaid,
                    ReceiptNumber = student.ReceiptNumber,

                    // Payment summary
                    TotalPayments = payments.Count,
                    TotalAmountPaid = paidPayments.Sum(p => p.Amount),
                    RecentPayments = payments.Take(5).Select(p => new StripePaymentResponseDto
                    {
                        StripePaymentId = p.StripePaymentId,
                        PaymentIntentId = p.PaymentIntentId,
                        ClientSecret = p.ClientSecret ?? string.Empty,
                        StudentId = p.StudentId,
                        InstallmentId = p.InstallmentId,
                        Amount = p.Amount,
                        Currency = p.Currency,
                        Status = p.Status,
                        StatusText = p.Status.ToString(),
                        PaymentMethod = p.PaymentMethod,
                        ErrorMessage = p.ErrorMessage,
                        CreatedAt = p.CreatedAt
                    }).ToList(),

                    // Document summary
                    TotalDocuments = documents.Count,
                    RequiredDocuments = 3,
                    Documents = documents.Select(d => new StudentDocumentDto
                    {
                        DocumentId = d.DocumentId,
                        StudentId = d.StudentId,
                        DocumentType = d.DocumentType.ToString(),
                        FileName = d.FileName,
                        FileSize = d.FileSize,
                        ContentType = d.ContentType,
                        UploadedAt = d.UploadedAt,
                        Description = d.Description,
                        DownloadUrl = $"/api/StudentDocument/{d.DocumentId}/download"
                    }).ToList()
                };

                return ResponseHelper.Success(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student detail");
                return ResponseHelper.Error<StudentDetailDto>("An error occurred while retrieving the student detail");
            }
        }

        public async Task<ApiResponse<List<StudentDto>>> GetAllStudentsAsync()
        {
            try
            {
                var students = await _context.Students
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                var studentDtos = students.Select(MapToDto).ToList();
                return ResponseHelper.Success(studentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving students");
                return ResponseHelper.Error<List<StudentDto>>("An error occurred while retrieving students");
            }
        }

        public async Task<ApiResponse<List<StudentDto>>> GetStudentsByStatusAsync(StudentStatus status)
        {
            try
            {
                var students = await _context.Students
                    .Where(s => s.Status == status)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                var studentDtos = students.Select(MapToDto).ToList();
                return ResponseHelper.Success(studentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving students by status");
                return ResponseHelper.Error<List<StudentDto>>("An error occurred while retrieving students");
            }
        }

        public async Task<ApiResponse<StudentDto>> UpdateStudentAsync(int studentId, UpdateStudentDto updateDto, string updatedBy)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);

                if (student == null)
                {
                    return ResponseHelper.Error<StudentDto>("Student not found");
                }

                // Check if email is being changed and if it already exists
                if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != student.Email)
                {
                    var normalizedEmail = NormalizeEmail(updateDto.Email);

                    var emailExists = await _context.Students
                        .AnyAsync(s => s.Email.ToLower() == normalizedEmail && s.StudentId != studentId);

                    if (emailExists)
                    {
                        return ResponseHelper.Error<StudentDto>($"A student with email '{updateDto.Email.Trim()}' already exists");
                    }
                }

                var changes = new List<string>();

                if (!string.IsNullOrEmpty(updateDto.Name) && updateDto.Name != student.Name)
                {
                    changes.Add($"Name: {student.Name} → {updateDto.Name}");
                    student.Name = updateDto.Name;
                }

                if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != student.Email)
                {
                    changes.Add($"Email: {student.Email} → {updateDto.Email}");
                    student.Email = updateDto.Email.Trim();
                }

                if (!string.IsNullOrEmpty(updateDto.Phone) && updateDto.Phone != student.Phone)
                {
                    changes.Add($"Phone: {student.Phone} → {updateDto.Phone}");
                    student.Phone = updateDto.Phone;
                }

                if (updateDto.CourseId.HasValue && updateDto.CourseId != student.CourseId)
                {
                    changes.Add($"CourseId: {student.CourseId} → {updateDto.CourseId}");
                    student.CourseId = updateDto.CourseId;
                }

                if (updateDto.BatchId.HasValue && updateDto.BatchId != student.BatchId)
                {
                    // Validate batch capacity if changing batch
                    if (updateDto.BatchId.Value > 0)
                    {
                        var capacityCheck = await ValidateBatchCapacityAsync(updateDto.BatchId.Value, student.StudentId);
                        if (!capacityCheck.IsSuccess)
                        {
                            return ResponseHelper.Error<StudentDto>(capacityCheck.ErrorMessage.FirstOrDefault() ?? "Batch capacity validation failed");
                        }
                        changes.Add($"BatchId: {student.BatchId} → {updateDto.BatchId}");
                        student.BatchId = updateDto.BatchId;
                    }
                    else
                    {
                        // BatchId = 0 or null means removing from batch
                        changes.Add($"BatchId: {student.BatchId} → null (removed from batch)");
                        student.BatchId = null;
                    }
                }

                if (updateDto.Status.HasValue && updateDto.Status != student.Status)
                {
                    changes.Add($"Status: {student.Status} → {updateDto.Status}");
                    student.Status = updateDto.Status.Value;
                }

                if (updateDto.Address != null && updateDto.Address != student.Address)
                {
                    changes.Add($"Address updated");
                    student.Address = updateDto.Address;
                }

                if (updateDto.EmergencyContact != null && updateDto.EmergencyContact != student.EmergencyContact)
                {
                    changes.Add($"Emergency contact updated");
                    student.EmergencyContact = updateDto.EmergencyContact;
                }

                if (updateDto.Notes != null && updateDto.Notes != student.Notes)
                {
                    changes.Add($"Notes updated");
                    student.Notes = updateDto.Notes;
                }

                if (updateDto.DocumentsPath != null && updateDto.DocumentsPath != student.DocumentsPath)
                {
                    changes.Add($"Documents path updated");
                    student.DocumentsPath = updateDto.DocumentsPath;
                }

                if (updateDto.AdmissionDate.HasValue && updateDto.AdmissionDate != student.AdmissionDate)
                {
                    changes.Add($"Admission date: {student.AdmissionDate?.ToString("yyyy-MM-dd")} → {updateDto.AdmissionDate?.ToString("yyyy-MM-dd")}");
                    student.AdmissionDate = updateDto.AdmissionDate;
                }

                if (updateDto.FeesTotal.HasValue && updateDto.FeesTotal != student.FeesTotal)
                {
                    changes.Add($"Total fees: {student.FeesTotal} → {updateDto.FeesTotal}");
                    student.FeesTotal = updateDto.FeesTotal.Value;
                }

                if (updateDto.FeesPaid.HasValue && updateDto.FeesPaid != student.FeesPaid)
                {
                    changes.Add($"Fees paid: {student.FeesPaid} → {updateDto.FeesPaid}");
                    student.FeesPaid = updateDto.FeesPaid.Value;
                }

                if (changes.Any())
                {
                    student.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    await _auditService.LogAsync(
                        ActionType.UPDATE,
                        "Student",
                        student.StudentId.ToString(),
                        null,
                        string.Join(", ", changes),
                        $"Updated student {student.Name}",
                        updatedBy
                    );
                }

                var studentDto = MapToDto(student);
                return ResponseHelper.Success(studentDto, "Student updated successfully");
            }
            catch (DbUpdateException ex) when (IsDuplicateEmailDbError(ex))
            {
                return ResponseHelper.Error<StudentDto>($"A student with email '{updateDto.Email?.Trim()}' already exists");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student");
                return ResponseHelper.Error<StudentDto>("An error occurred while updating the student");
            }
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLower();
        }

        private static bool IsDuplicateEmailDbError(DbUpdateException ex)
        {
            var allMessages = ex.ToString().ToLowerInvariant();
            return allMessages.Contains("email") &&
                   (allMessages.Contains("duplicate") || allMessages.Contains("unique") || allMessages.Contains("2601") || allMessages.Contains("2627"));
        }

        public async Task<ApiResponse<bool>> DeleteStudentAsync(int studentId, string deletedBy)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);

                if (student == null)
                {
                    return ResponseHelper.Error<bool>("Student not found");
                }

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    ActionType.DELETE,
                    "Student",
                    student.StudentId.ToString(),
                    $"{student.Name} ({student.Email})",
                    null,
                    null,
                    deletedBy
                );

                return ResponseHelper.Success(true, "Student deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student");
                return ResponseHelper.Error<bool>("An error occurred while deleting the student");
            }
        }

        public async Task<ApiResponse<bool>> ChangeStudentStatusAsync(int studentId, StudentStatus status, string updatedBy)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);

                if (student == null)
                {
                    return ResponseHelper.Error<bool>("Student not found");
                }

                var oldStatus = student.Status;
                student.Status = status;
                student.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Student",
                    student.StudentId.ToString(),
                    oldStatus.ToString(),
                    status.ToString(),
                    $"Changed status of {student.Name} from {oldStatus} to {status}",
                    updatedBy
                );

                return ResponseHelper.Success(true, "Student status updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing student status");
                return ResponseHelper.Error<bool>("An error occurred while changing student status");
            }
        }

        public async Task<ApiResponse<StudentDto>> GetStudentByEmailAsync(string email)
        {
            try
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Email == email);

                if (student == null)
                {
                    return ResponseHelper.Error<StudentDto>("Student not found");
                }

                var studentDto = MapToDto(student);
                return ResponseHelper.Success(studentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student by email");
                return ResponseHelper.Error<StudentDto>("An error occurred while retrieving the student");
            }
        }

        public async Task<ApiResponse<RegistrationSummaryDto>> GetRegistrationSummaryAsync(int studentId)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);

                if (student == null)
                {
                    return ResponseHelper.Error<RegistrationSummaryDto>("Student not found");
                }

                string? courseName = null;
                decimal courseFee = 0;
                if (student.CourseId.HasValue)
                {
                    var course = await _context.Courses.FindAsync(student.CourseId.Value);
                    courseName = course?.Name;
                    courseFee = course?.Fees ?? 0;
                }

                string? batchName = null;
                if (student.BatchId.HasValue)
                {
                    var batch = await _context.Batches.FindAsync(student.BatchId.Value);
                    batchName = batch?.Name;
                }

                var documents = await _context.StudentDocuments
                    .Where(d => d.StudentId == studentId)
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();

                var summary = new RegistrationSummaryDto
                {
                    StudentId = student.StudentId,
                    Name = student.Name,
                    Email = student.Email,
                    Phone = student.Phone,
                    CourseName = courseName,
                    BatchName = batchName,
                    CourseFee = courseFee,
                    FeesTotal = student.FeesTotal,
                    Status = student.Status.ToString(),
                    Notes = student.Notes,
                    DocumentsUploaded = documents.Count,
                    RequiredDocuments = 3,
                    Documents = documents.Select(d => new StudentDocumentDto
                    {
                        DocumentId = d.DocumentId,
                        StudentId = d.StudentId,
                        DocumentType = d.DocumentType.ToString(),
                        FileName = d.FileName,
                        FileSize = d.FileSize,
                        ContentType = d.ContentType,
                        UploadedAt = d.UploadedAt,
                        Description = d.Description,
                        DownloadUrl = $"/api/StudentDocument/{d.DocumentId}/download"
                    }).ToList()
                };

                return ResponseHelper.Success(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving registration summary");
                return ResponseHelper.Error<RegistrationSummaryDto>("An error occurred while retrieving the registration summary");
            }
        }

        public async Task<ApiResponse<StudentDto>> ProcessCashPaymentAsync(int studentId, CashPaymentDto dto, string processedBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var student = await _context.Students.FindAsync(studentId);

                if (student == null)
                {
                    return ResponseHelper.Error<StudentDto>("Student not found");
                }

                if (dto.Amount <= 0)
                {
                    return ResponseHelper.Error<StudentDto>("Payment amount must be greater than zero");
                }

                var oldFeesPaid = student.FeesPaid;
                student.FeesPaid += dto.Amount;
                student.UpdatedAt = DateTime.UtcNow;

                // If student is PendingPayment, complete admission
                if (student.Status == StudentStatus.PendingPayment)
                {
                    student.Status = StudentStatus.Enrolled;
                    student.AdmissionDate = DateTime.UtcNow;

                    // Generate fresh credentials
                    var tempPassword = GenerateTemporaryPassword();
                    student.PasswordHash = _passwordHasher.HashPassword(student, tempPassword);

                    // Send single combined email with admission confirmation + credentials
                    try
                    {
                        await _emailService.SendAdmissionConfirmationEmailAsync(student.Email, student, tempPassword);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Failed to send admission confirmation email to {Email}", student.Email);
                    }
                }

                // Save cash payment record so it appears in financial reports
                var cashPaymentRecord = new CashPayment
                {
                    StudentId = student.StudentId,
                    Amount = dto.Amount,
                    Remarks = dto.Remarks,
                    ProcessedBy = processedBy,
                    PaidAt = DateTime.UtcNow
                };
                _context.CashPayments.Add(cashPaymentRecord);

                await _context.SaveChangesAsync();

                // Log the action
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Student",
                    student.StudentId.ToString(),
                    $"FeesPaid: {oldFeesPaid}, Status: PendingPayment",
                    $"FeesPaid: {student.FeesPaid}, Status: {student.Status}, CashAmount: {dto.Amount}",
                    $"Cash payment of {dto.Amount} processed for {student.Name}. {dto.Remarks ?? ""}",
                    processedBy
                );

                await transaction.CommitAsync();

                var studentDto = MapToDto(student);
                return ResponseHelper.Success(studentDto, $"Cash payment of {dto.Amount} processed successfully. Student status: {student.Status}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing cash payment for student {StudentId}", studentId);
                return ResponseHelper.Error<StudentDto>("An error occurred while processing the cash payment");
            }
        }

        private StudentDto MapToDto(Student student)
        {
            return new StudentDto
            {
                StudentId = student.StudentId,
                Name = student.Name,
                Email = student.Email,
                Phone = student.Phone,
                CourseId = student.CourseId,
                BatchId = student.BatchId,
                Status = student.Status.ToString(),
                DocumentsPath = student.DocumentsPath,
                Address = student.Address,
                EmergencyContact = student.EmergencyContact,
                CreatedAt = student.CreatedAt,
                UpdatedAt = student.UpdatedAt,
                AdmissionDate = student.AdmissionDate,
                FeesPaid = student.FeesPaid,
                FeesTotal = student.FeesTotal,
                FeesRemaining = student.FeesTotal - student.FeesPaid,
                ReceiptNumber = student.ReceiptNumber,
                Notes = student.Notes
            };
        }

        public async Task<ApiResponse<List<CashPaymentRecordDto>>> GetCashPaymentsByStudentIdAsync(int studentId)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return ResponseHelper.Error<List<CashPaymentRecordDto>>("Student not found");

                var payments = await _context.CashPayments
                    .Where(c => c.StudentId == studentId)
                    .OrderByDescending(c => c.PaidAt)
                    .Select(c => new CashPaymentRecordDto
                    {
                        CashPaymentId = c.CashPaymentId,
                        StudentId = c.StudentId,
                        StudentName = student.Name,
                        Amount = c.Amount,
                        Remarks = c.Remarks,
                        ProcessedBy = c.ProcessedBy,
                        PaidAt = c.PaidAt
                    })
                    .ToListAsync();

                return ResponseHelper.Success(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cash payments for student {StudentId}", studentId);
                return ResponseHelper.Error<List<CashPaymentRecordDto>>("An error occurred while retrieving cash payments");
            }
        }

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task<ApiResponse<bool>> ValidateBatchCapacityAsync(int batchId, int? excludeStudentId)
        {
            try
            {
                var batch = await _context.Batches
                    .FirstOrDefaultAsync(b => b.BatchId == batchId);

                if (batch == null)
                {
                    return ResponseHelper.Error<bool>("Batch not found");
                }

                if (!batch.IsActive)
                {
                    return ResponseHelper.Error<bool>("Batch is not active");
                }

                // Count current students (excluding the student being updated if applicable)
                var currentStudentCount = await _context.Students
                    .CountAsync(s => s.BatchId == batchId && (!excludeStudentId.HasValue || s.StudentId != excludeStudentId.Value));

                if (currentStudentCount >= batch.MaxStudents)
                {
                    return ResponseHelper.Error<bool>($"Batch is full. Maximum capacity: {batch.MaxStudents}, Current: {currentStudentCount}");
                }

                return ResponseHelper.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating batch capacity");
                return ResponseHelper.Error<bool>("Failed to validate batch capacity");
            }
        }
    }
}
