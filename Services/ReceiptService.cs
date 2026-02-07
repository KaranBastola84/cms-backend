using JWTAuthAPI.Data;
using JWTAuthAPI.Helpers;
using JWTAuthAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace JWTAuthAPI.Services
{
    public class ReceiptService : IReceiptService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IEmailService _emailService;
        private readonly ILogger<ReceiptService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly string _receiptsFolder;

        public ReceiptService(
            ApplicationDbContext context,
            IAuditService auditService,
            IEmailService emailService,
            ILogger<ReceiptService> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _auditService = auditService;
            _emailService = emailService;
            _logger = logger;
            _environment = environment;
            _receiptsFolder = Path.Combine(_environment.ContentRootPath, "Uploads", "Receipts");

            // Ensure receipts directory exists
            if (!Directory.Exists(_receiptsFolder))
            {
                Directory.CreateDirectory(_receiptsFolder);
            }
        }

        public async Task<ApiResponse<ReceiptDto>> GenerateReceiptAsync(CreateReceiptDto createDto, string generatedBy)
        {
            try
            {
                // Get student details
                var student = await _context.Students.FindAsync(createDto.StudentId);
                if (student == null)
                {
                    return ResponseHelper.Error<ReceiptDto>("Student not found");
                }

                // Generate receipt number
                var receiptNumber = await GenerateReceiptNumberAsync();

                // Create receipt record
                var receipt = new Receipt
                {
                    ReceiptNumber = receiptNumber,
                    StudentId = createDto.StudentId,
                    Amount = createDto.Amount,
                    ReceiptType = createDto.ReceiptType,
                    Description = createDto.Description,
                    PaymentDate = createDto.PaymentDate ?? DateTime.UtcNow,
                    PaymentMethod = createDto.PaymentMethod,
                    GeneratedAt = DateTime.UtcNow,
                    GeneratedBy = generatedBy,
                    FilePath = "" // Will be set after PDF generation
                };

                // Generate PDF
                var pdfPath = await GenerateReceiptPdfAsync(receipt, student);
                receipt.FilePath = pdfPath;

                _context.Receipts.Add(receipt);
                await _context.SaveChangesAsync();

                // Update student's receipt number if this is admission fee
                if (createDto.ReceiptType == ReceiptType.AdmissionFee && string.IsNullOrEmpty(student.ReceiptNumber))
                {
                    student.ReceiptNumber = receiptNumber;
                    await _context.SaveChangesAsync();

                    // Send admission confirmation email with receipt attached
                    try
                    {
                        await _emailService.SendAdmissionConfirmationEmailAsync(student.Email, student, pdfPath);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Failed to send admission confirmation email to {Email}", student.Email);
                    }
                }

                // Log the action
                await _auditService.LogAsync(
                    ActionType.CREATE,
                    "Receipt",
                    receipt.ReceiptId.ToString(),
                    null,
                    $"Generated {createDto.ReceiptType} receipt for {student.Name}",
                    $"Amount: {createDto.Amount}, Receipt#: {receiptNumber}",
                    generatedBy
                );

                var receiptDto = await MapToDtoAsync(receipt);
                return ResponseHelper.Success(receiptDto, "Receipt generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating receipt");
                return ResponseHelper.Error<ReceiptDto>("An error occurred while generating the receipt");
            }
        }

        public async Task<ApiResponse<ReceiptDto>> GetReceiptByIdAsync(int receiptId)
        {
            try
            {
                var receipt = await _context.Receipts
                    .Include(r => r.Student)
                    .FirstOrDefaultAsync(r => r.ReceiptId == receiptId);

                if (receipt == null)
                {
                    return ResponseHelper.Error<ReceiptDto>("Receipt not found");
                }

                var receiptDto = await MapToDtoAsync(receipt);
                return ResponseHelper.Success(receiptDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving receipt");
                return ResponseHelper.Error<ReceiptDto>("An error occurred while retrieving the receipt");
            }
        }

        public async Task<ApiResponse<List<ReceiptDto>>> GetReceiptsByStudentIdAsync(int studentId)
        {
            try
            {
                var receipts = await _context.Receipts
                    .Include(r => r.Student)
                    .Where(r => r.StudentId == studentId)
                    .OrderByDescending(r => r.GeneratedAt)
                    .ToListAsync();

                var receiptDtos = new List<ReceiptDto>();
                foreach (var receipt in receipts)
                {
                    receiptDtos.Add(await MapToDtoAsync(receipt));
                }

                return ResponseHelper.Success(receiptDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving receipts");
                return ResponseHelper.Error<List<ReceiptDto>>("An error occurred while retrieving receipts");
            }
        }

        public async Task<ApiResponse<ReceiptDto>> GetReceiptByNumberAsync(string receiptNumber)
        {
            try
            {
                var receipt = await _context.Receipts
                    .Include(r => r.Student)
                    .FirstOrDefaultAsync(r => r.ReceiptNumber == receiptNumber);

                if (receipt == null)
                {
                    return ResponseHelper.Error<ReceiptDto>("Receipt not found");
                }

                var receiptDto = await MapToDtoAsync(receipt);
                return ResponseHelper.Success(receiptDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving receipt by number");
                return ResponseHelper.Error<ReceiptDto>("An error occurred while retrieving the receipt");
            }
        }

        public async Task<ApiResponse<(byte[] FileData, string ContentType, string FileName)>> DownloadReceiptAsync(int receiptId)
        {
            try
            {
                var receipt = await _context.Receipts.FindAsync(receiptId);

                if (receipt == null)
                {
                    return ResponseHelper.Error<(byte[], string, string)>("Receipt not found");
                }

                if (!File.Exists(receipt.FilePath))
                {
                    return ResponseHelper.Error<(byte[], string, string)>("Receipt file not found on server");
                }

                var fileData = await File.ReadAllBytesAsync(receipt.FilePath);
                var fileName = $"Receipt_{receipt.ReceiptNumber}.html";
                return ResponseHelper.Success((fileData, "text/html", fileName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading receipt");
                return ResponseHelper.Error<(byte[], string, string)>("An error occurred while downloading the receipt");
            }
        }

        public async Task<ApiResponse<bool>> DeleteReceiptAsync(int receiptId, string deletedBy)
        {
            try
            {
                var receipt = await _context.Receipts.FindAsync(receiptId);

                if (receipt == null)
                {
                    return ResponseHelper.Error<bool>("Receipt not found");
                }

                // Delete physical file
                if (File.Exists(receipt.FilePath))
                {
                    File.Delete(receipt.FilePath);
                }

                // Delete database record
                _context.Receipts.Remove(receipt);
                await _context.SaveChangesAsync();

                // Log the action
                await _auditService.LogAsync(
                    ActionType.DELETE,
                    "Receipt",
                    receipt.ReceiptId.ToString(),
                    $"{receipt.ReceiptNumber} - {receipt.Amount}",
                    null,
                    $"Deleted receipt for student ID {receipt.StudentId}",
                    deletedBy
                );

                return ResponseHelper.Success(true, "Receipt deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting receipt");
                return ResponseHelper.Error<bool>("An error occurred while deleting the receipt");
            }
        }

        private async Task<string> GenerateReceiptNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month;
            var prefix = $"RCP{year}{month:D2}";

            var lastReceipt = await _context.Receipts
                .Where(r => r.ReceiptNumber.StartsWith(prefix))
                .OrderByDescending(r => r.ReceiptNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastReceipt != null)
            {
                var lastNumberStr = lastReceipt.ReceiptNumber.Substring(prefix.Length);
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D5}";
        }

        private async Task<string> GenerateReceiptPdfAsync(Receipt receipt, Student student)
        {
            var fileName = $"Receipt_{receipt.ReceiptNumber}.html";
            var filePath = Path.Combine(_receiptsFolder, fileName);

            var html = GenerateReceiptHtml(receipt, student);
            await File.WriteAllTextAsync(filePath, html);

            return filePath;
        }

        private string GenerateReceiptHtml(Receipt receipt, Student student)
        {
            var paymentDate = receipt.PaymentDate?.ToString("MMMM dd, yyyy") ?? DateTime.UtcNow.ToString("MMMM dd, yyyy");
            var admissionDate = student.AdmissionDate?.ToString("MMMM dd, yyyy") ?? "N/A";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Receipt - {receipt.ReceiptNumber}</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ font-family: Arial, sans-serif; padding: 40px; background-color: #f5f5f5; }}
        .receipt {{ max-width: 800px; margin: 0 auto; background-color: white; padding: 40px; border: 2px solid #6B4423; }}
        .header {{ text-align: center; border-bottom: 3px solid #6B4423; padding-bottom: 20px; margin-bottom: 30px; }}
        .header h1 {{ color: #6B4423; font-size: 32px; margin-bottom: 5px; }}
        .header p {{ color: #666; font-size: 14px; }}
        .receipt-number {{ text-align: right; margin-bottom: 20px; font-size: 14px; color: #666; }}
        .receipt-number strong {{ color: #6B4423; font-size: 16px; }}
        .section {{ margin-bottom: 25px; }}
        .section-title {{ background-color: #6B4423; color: white; padding: 10px; font-weight: bold; margin-bottom: 15px; }}
        .info-row {{ display: flex; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .info-label {{ flex: 0 0 180px; font-weight: bold; color: #333; }}
        .info-value {{ flex: 1; color: #666; }}
        .amount-section {{ background-color: #f9f9f9; padding: 20px; margin: 30px 0; border-left: 4px solid #6B4423; }}
        .amount-row {{ display: flex; justify-content: space-between; padding: 10px 0; font-size: 18px; }}
        .amount-label {{ font-weight: bold; color: #333; }}
        .amount-value {{ color: #6B4423; font-weight: bold; font-size: 24px; }}
        .footer {{ margin-top: 40px; padding-top: 20px; border-top: 2px solid #ddd; text-align: center; }}
        .footer p {{ color: #666; font-size: 12px; line-height: 1.6; }}
        .signature {{ margin-top: 50px; display: flex; justify-content: space-between; }}
        .signature-box {{ text-align: center; }}
        .signature-line {{ border-top: 2px solid #333; padding-top: 5px; margin-top: 40px; width: 200px; }}
        .watermark {{ position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%) rotate(-45deg); font-size: 100px; color: rgba(107, 68, 35, 0.05); font-weight: bold; z-index: -1; }}
    </style>
</head>
<body>
    <div class='receipt'>
        <div class='watermark'>COFFEE SCHOOL</div>
        
        <div class='header'>
            <h1>☕ COFFEE SCHOOL</h1>
            <p>Premium Coffee Education & Training Institute</p>
            <p>Email: info@coffeeschool.com | Phone: +1 (555) 123-4567</p>
        </div>

        <div class='receipt-number'>
            <p><strong>Receipt Number:</strong> {receipt.ReceiptNumber}</p>
            <p>Generated: {receipt.GeneratedAt.ToString("MMMM dd, yyyy hh:mm tt")}</p>
        </div>

        <div class='section'>
            <div class='section-title'>STUDENT INFORMATION</div>
            <div class='info-row'>
                <div class='info-label'>Student ID:</div>
                <div class='info-value'>{student.StudentId}</div>
            </div>
            <div class='info-row'>
                <div class='info-label'>Student Name:</div>
                <div class='info-value'>{student.Name}</div>
            </div>
            <div class='info-row'>
                <div class='info-label'>Email:</div>
                <div class='info-value'>{student.Email}</div>
            </div>
            <div class='info-row'>
                <div class='info-label'>Phone:</div>
                <div class='info-value'>{student.Phone}</div>
            </div>
            <div class='info-row'>
                <div class='info-label'>Admission Date:</div>
                <div class='info-value'>{admissionDate}</div>
            </div>
        </div>

        <div class='section'>
            <div class='section-title'>PAYMENT DETAILS</div>
            <div class='info-row'>
                <div class='info-label'>Receipt Type:</div>
                <div class='info-value'>{receipt.ReceiptType}</div>
            </div>
            <div class='info-row'>
                <div class='info-label'>Payment Date:</div>
                <div class='info-value'>{paymentDate}</div>
            </div>
            <div class='info-row'>
                <div class='info-label'>Payment Method:</div>
                <div class='info-value'>{receipt.PaymentMethod ?? "N/A"}</div>
            </div>
            {(string.IsNullOrEmpty(receipt.Description) ? "" : $@"
            <div class='info-row'>
                <div class='info-label'>Description:</div>
                <div class='info-value'>{receipt.Description}</div>
            </div>")}
        </div>

        <div class='amount-section'>
            <div class='amount-row'>
                <div class='amount-label'>AMOUNT PAID:</div>
                <div class='amount-value'>${receipt.Amount:N2}</div>
            </div>
        </div>

        <div class='section'>
            <div class='section-title'>FEE SUMMARY</div>
            <div class='info-row'>
                <div class='info-label'>Total Fees:</div>
                <div class='info-value'>${student.FeesTotal:N2}</div>
            </div>
            <div class='info-row'>
                <div class='info-label'>Fees Paid:</div>
                <div class='info-value'>${student.FeesPaid:N2}</div>
            </div>
            <div class='info-row'>
                <div class='info-label'>Balance Remaining:</div>
                <div class='info-value' style='color: #d32f2f; font-weight: bold;'>${student.FeesTotal - student.FeesPaid:N2}</div>
            </div>
        </div>

        <div class='signature'>
            <div class='signature-box'>
                <div class='signature-line'>Authorized Signatory</div>
            </div>
            <div class='signature-box'>
                <div class='signature-line'>Student Signature</div>
            </div>
        </div>

        <div class='footer'>
            <p><strong>Terms & Conditions:</strong></p>
            <p>This is a computer-generated receipt and does not require a physical signature.</p>
            <p>Please keep this receipt for your records. Fees once paid are non-refundable.</p>
            <p>For any queries, please contact our administration office.</p>
            <p style='margin-top: 20px;'>&copy; {DateTime.UtcNow.Year} Coffee School. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private async Task<ReceiptDto> MapToDtoAsync(Receipt receipt)
        {
            var student = receipt.Student ?? await _context.Students.FindAsync(receipt.StudentId);

            return new ReceiptDto
            {
                ReceiptId = receipt.ReceiptId,
                ReceiptNumber = receipt.ReceiptNumber,
                StudentId = receipt.StudentId,
                StudentName = student?.Name ?? "Unknown",
                StudentEmail = student?.Email ?? "Unknown",
                Amount = receipt.Amount,
                ReceiptType = receipt.ReceiptType.ToString(),
                GeneratedAt = receipt.GeneratedAt,
                GeneratedBy = receipt.GeneratedBy,
                Description = receipt.Description,
                PaymentDate = receipt.PaymentDate,
                PaymentMethod = receipt.PaymentMethod,
                DownloadUrl = $"/api/Receipt/{receipt.ReceiptId}/download"
            };
        }
    }
}
