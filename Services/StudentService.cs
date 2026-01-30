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
        private readonly ILogger<StudentService> _logger;

        public StudentService(
            ApplicationDbContext context,
            IPasswordHasher<Student> passwordHasher,
            IAuditService auditService,
            ILogger<StudentService> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<ApiResponse<StudentDto>> CreateStudentAsync(CreateStudentDto createDto, string createdBy)
        {
            try
            {
                // Check if email already exists
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.Email == createDto.Email);

                if (existingStudent != null)
                {
                    return ResponseHelper.Error<StudentDto>("A student with this email already exists");
                }

                var student = new Student
                {
                    Name = createDto.Name,
                    Email = createDto.Email,
                    Phone = createDto.Phone,
                    CourseId = createDto.CourseId,
                    BatchId = createDto.BatchId,
                    Address = createDto.Address,
                    EmergencyContact = createDto.EmergencyContact,
                    DocumentsPath = createDto.DocumentsPath,
                    Status = StudentStatus.Enrolled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Generate a random password for the student portal
                var tempPassword = GenerateTemporaryPassword();
                student.PasswordHash = _passwordHasher.HashPassword(student, tempPassword);

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // Log the action
                await _auditService.LogAsync(
                    ActionType.CREATE,
                    "Student",
                    student.StudentId.ToString(),
                    null,
                    $"Created new student: {student.Name} ({student.Email})",
                    $"Temporary password: {tempPassword}",
                    createdBy
                );

                var studentDto = MapToDto(student);
                return ResponseHelper.Success(studentDto, "Student created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student");
                return ResponseHelper.Error<StudentDto>("An error occurred while creating the student");
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
                    var emailExists = await _context.Students
                        .AnyAsync(s => s.Email == updateDto.Email && s.StudentId != studentId);

                    if (emailExists)
                    {
                        return ResponseHelper.Error<StudentDto>("A student with this email already exists");
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
                    student.Email = updateDto.Email;
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
                    changes.Add($"BatchId: {student.BatchId} → {updateDto.BatchId}");
                    student.BatchId = updateDto.BatchId;
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

                if (updateDto.DocumentsPath != null && updateDto.DocumentsPath != student.DocumentsPath)
                {
                    changes.Add($"Documents path updated");
                    student.DocumentsPath = updateDto.DocumentsPath;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student");
                return ResponseHelper.Error<StudentDto>("An error occurred while updating the student");
            }
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
                UpdatedAt = student.UpdatedAt
            };
        }

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
