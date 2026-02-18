using System.Net;
using System.Net.Mail;
using JWTAuthAPI.Models;

namespace JWTAuthAPI.Services
{
    public interface IEmailService
    {
        Task SendInquiryConfirmationEmailAsync(string toEmail, string fullName);
        Task SendOTPEmailAsync(string toEmail, string fullName, string otp);
        Task SendAccountActivationEmailAsync(string toEmail, string fullName, string email);
        Task SendStudentCredentialsEmailAsync(string toEmail, string studentName, string password);
        Task SendAdmissionConfirmationEmailAsync(string toEmail, Student student, string? receiptPath = null);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendInquiryConfirmationEmailAsync(string toEmail, string fullName)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var fromEmail = smtpSettings["FromEmail"] ?? throw new InvalidOperationException("SMTP FromEmail is not configured");
                var fromName = smtpSettings["FromName"] ?? "Coffee School";
                var smtpServer = smtpSettings["Server"] ?? throw new InvalidOperationException("SMTP Server is not configured");
                var smtpPort = int.Parse(smtpSettings["Port"] ?? "587");
                var smtpUsername = smtpSettings["Username"] ?? throw new InvalidOperationException("SMTP Username is not configured");
                var smtpPassword = smtpSettings["Password"] ?? throw new InvalidOperationException("SMTP Password is not configured");
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = enableSsl;
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = "Thank you for contacting Coffee School",
                        IsBodyHtml = true,
                        Body = GetEmailBody(fullName)
                    };

                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Confirmation email sent successfully to {toEmail}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send confirmation email to {toEmail}: {ex.Message}");
                // Don't throw - we don't want email failures to prevent inquiry submission
            }
        }

        private string GetEmailBody(string fullName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background-color: #6F4E37;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .content {{
            background-color: #f9f9f9;
            padding: 30px;
            border-radius: 0 0 5px 5px;
        }}
        .footer {{
            text-align: center;
            margin-top: 20px;
            font-size: 12px;
            color: #666;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Coffee School</h1>
        </div>
        <div class='content'>
            <h2>Thank you for contacting us!</h2>
            <p>Dear {fullName},</p>
            <p>Thank you for contacting Coffee School. We have received your inquiry and our staff will reach out soon.</p>
            <p>We appreciate your interest in our coffee courses and look forward to helping you on your coffee journey!</p>
            <p>Best regards,<br>
            The Coffee School Team</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.UtcNow.Year} Coffee School. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        public async Task SendOTPEmailAsync(string toEmail, string fullName, string otp)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var fromEmail = smtpSettings["FromEmail"] ?? throw new InvalidOperationException("SMTP FromEmail is not configured");
                var fromName = smtpSettings["FromName"] ?? "Coffee School";
                var smtpServer = smtpSettings["Server"] ?? throw new InvalidOperationException("SMTP Server is not configured");
                var smtpPort = int.Parse(smtpSettings["Port"] ?? "587");
                var smtpUsername = smtpSettings["Username"] ?? throw new InvalidOperationException("SMTP Username is not configured");
                var smtpPassword = smtpSettings["Password"] ?? throw new InvalidOperationException("SMTP Password is not configured");
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = enableSsl;
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = "Your Account Verification Code",
                        IsBodyHtml = true,
                        Body = GetOTPEmailBody(fullName, otp)
                    };

                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"OTP email sent successfully to {toEmail}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send OTP email to {toEmail}: {ex.Message}");
                throw; // We want to know if OTP email fails
            }
        }

        private string GetOTPEmailBody(string fullName, string otp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background-color: #6F4E37;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .content {{
            background-color: #f9f9f9;
            padding: 30px;
            border-radius: 0 0 5px 5px;
        }}
        .otp-box {{
            background-color: #fff;
            border: 2px solid #6F4E37;
            padding: 20px;
            text-align: center;
            margin: 20px 0;
            border-radius: 5px;
        }}
        .otp-code {{
            font-size: 32px;
            font-weight: bold;
            color: #6F4E37;
            letter-spacing: 5px;
        }}
        .footer {{
            text-align: center;
            margin-top: 20px;
            font-size: 12px;
            color: #666;
        }}
        .warning {{
            color: #d9534f;
            font-size: 14px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Coffee School Management Portal</h1>
        </div>
        <div class='content'>
            <h2>Welcome to Coffee School!</h2>
            <p>Dear {fullName},</p>
            <p>An administrator has created an account for you. Please use the verification code below to complete your account setup:</p>
            
            <div class='otp-box'>
                <p style='margin: 0; font-size: 14px;'>Your Verification Code:</p>
                <p class='otp-code'>{otp}</p>
            </div>
            
            <p><strong>This code will expire in 15 minutes.</strong></p>
            <p>After verifying this code, you will be able to set your password. Please wait for the administrator to activate your account before you can log in.</p>
            
            <p class='warning'>⚠️ If you did not request this account, please contact the administrator immediately.</p>
            
            <p>Best regards,<br>
            The Coffee School Team</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.UtcNow.Year} Coffee School. All rights reserved.</p>
        </div>
    </div>
</body>
// Activation Email HTML Template
</html>";
        }

        public async Task SendAccountActivationEmailAsync(string toEmail, string fullName, string email)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var fromEmail = smtpSettings["FromEmail"] ?? throw new InvalidOperationException("SMTP FromEmail is not configured");
                var fromName = smtpSettings["FromName"] ?? "Coffee School";
                var smtpServer = smtpSettings["Server"] ?? throw new InvalidOperationException("SMTP Server is not configured");
                var smtpPort = int.Parse(smtpSettings["Port"] ?? "587");
                var smtpUsername = smtpSettings["Username"] ?? throw new InvalidOperationException("SMTP Username is not configured");
                var smtpPassword = smtpSettings["Password"] ?? throw new InvalidOperationException("SMTP Password is not configured");
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = enableSsl;
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = "Your Account is Now Active!",
                        IsBodyHtml = true,
                        Body = GetActivationEmailBody(fullName, email)
                    };

                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Activation email sent successfully to {toEmail}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send activation email to {toEmail}: {ex.Message}");
                throw;
            }
        }

        private string GetActivationEmailBody(string fullName, string email)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background-color: #6F4E37;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .content {{
            background-color: #f9f9f9;
            padding: 30px;
            border-radius: 0 0 5px 5px;
        }}
        .success-box {{
            background-color: #d4edda;
            border: 2px solid #28a745;
            padding: 20px;
            text-align: center;
            margin: 20px 0;
            border-radius: 5px;
        }}
        .success-icon {{
            font-size: 48px;
            color: #28a745;
        }}
        .credentials-box {{
            background-color: #fff;
            border: 1px solid #ddd;
            padding: 15px;
            margin: 20px 0;
            border-radius: 5px;
        }}
        .footer {{
            text-align: center;
            margin-top: 20px;
            font-size: 12px;
            color: #666;
        }}
        .button {{
            display: inline-block;
            background-color: #6F4E37;
            color: white;
            padding: 12px 30px;
            text-decoration: none;
            border-radius: 5px;
            margin: 10px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Coffee School Management Portal</h1>
        </div>
        <div class='content'>
            <div class='success-box'>
                <div class='success-icon'>✓</div>
                <h2 style='color: #28a745; margin: 10px 0;'>Account Activated!</h2>
            </div>
            
            <p>Dear {fullName},</p>
            <p>Great news! Your account has been activated by the administrator. You can now log in to the Coffee School management portal.</p>
            
            <div class='credentials-box'>
                <p style='margin: 5px 0;'><strong>Your Login Email:</strong></p>
                <p style='margin: 5px 0; color: #6F4E37; font-size: 18px;'>{email}</p>
            </div>
            
            <p>Use the email address above along with the password you set during verification to log in to the system.</p>
            
            <p><strong>What's next?</strong></p>
            <ul>
                <li>Log in to the management portal</li>
                <li>Complete your profile information</li>
                <li>Familiarize yourself with the system</li>
                <li>Contact your supervisor if you need assistance</li>
            </ul>
            
            <p>If you have any questions or encounter any issues, please contact the administrator or your supervisor.</p>
            
            <p>Welcome to the Coffee School team!</p>
            
            <p>Best regards,<br>
            The Coffee School Administration Team</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.UtcNow.Year} Coffee School. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        public async Task SendStudentCredentialsEmailAsync(string toEmail, string studentName, string password)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var fromEmail = smtpSettings["FromEmail"] ?? throw new InvalidOperationException("SMTP FromEmail is not configured");
                var fromName = smtpSettings["FromName"] ?? "Coffee School";
                var smtpServer = smtpSettings["Server"] ?? throw new InvalidOperationException("SMTP Server is not configured");
                var smtpPort = int.Parse(smtpSettings["Port"] ?? "587");
                var smtpUsername = smtpSettings["Username"] ?? throw new InvalidOperationException("SMTP Username is not configured");
                var smtpPassword = smtpSettings["Password"] ?? throw new InvalidOperationException("SMTP Password is not configured");
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = enableSsl;
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = "Welcome to Coffee School - Your Student Portal Credentials",
                        IsBodyHtml = true,
                        Body = GetStudentCredentialsEmailBody(studentName, toEmail, password)
                    };

                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation("Student credentials email sent successfully to {Email}", toEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send student credentials email to {Email}", toEmail);
                throw;
            }
        }

        private string GetStudentCredentialsEmailBody(string studentName, string email, string password)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Welcome to Coffee School</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
    <table role='presentation' style='width: 100%; border-collapse: collapse;'>
        <tr>
            <td style='padding: 40px 0; text-align: center; background: linear-gradient(135deg, #6B4423 0%, #8B6F47 100%);'>
                <h1 style='color: #ffffff; margin: 0; font-size: 28px;'>☕ Coffee School</h1>
            </td>
        </tr>
        <tr>
            <td style='padding: 0;'>
                <table role='presentation' style='width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                    <tr>
                        <td style='padding: 40px 30px;'>
                            <h2 style='color: #333333; margin: 0 0 20px 0;'>Welcome to Coffee School! 🎉</h2>
                            <p style='color: #666666; font-size: 16px; line-height: 1.5; margin: 0 0 20px 0;'>
                                Dear <strong>{studentName}</strong>,
                            </p>
                            <p style='color: #666666; font-size: 16px; line-height: 1.5; margin: 0 0 20px 0;'>
                                Congratulations on your enrollment! Your student account has been successfully created. 
                                Below are your login credentials for the Student Portal:
                            </p>
                            
                            <div style='background-color: #f8f8f8; border-left: 4px solid #6B4423; padding: 20px; margin: 20px 0; border-radius: 4px;'>
                                <p style='margin: 0 0 10px 0; color: #333333;'><strong>Email:</strong></p>
                                <p style='margin: 0 0 20px 0; color: #666666; font-family: monospace; font-size: 14px;'>{email}</p>
                                
                                <p style='margin: 0 0 10px 0; color: #333333;'><strong>Temporary Password:</strong></p>
                                <p style='margin: 0; color: #6B4423; font-family: monospace; font-size: 18px; font-weight: bold;'>{password}</p>
                            </div>

                            <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                                <p style='margin: 0; color: #856404; font-size: 14px;'>
                                    ⚠️ <strong>Important:</strong> Please change your password after your first login for security purposes.
                                </p>
                            </div>

                            <p style='color: #666666; font-size: 16px; line-height: 1.5; margin: 20px 0;'>
                                If you have any questions or need assistance, please don't hesitate to contact our support team.
                            </p>

                            <p style='color: #666666; font-size: 16px; line-height: 1.5; margin: 20px 0 0 0;'>
                                Best regards,<br>
                                <strong>Coffee School Team</strong>
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
        <tr>
            <td style='padding: 20px; text-align: center;'>
                <p style='color: #999999; font-size: 12px; margin: 0;'>
                    © {DateTime.UtcNow.Year} Coffee School. All rights reserved.
                </p>
                <p style='color: #999999; font-size: 12px; margin: 5px 0 0 0;'>
                    This is an automated email. Please do not reply to this message.
                </p>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        public async Task SendAdmissionConfirmationEmailAsync(string toEmail, Student student, string? receiptPath = null)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var fromEmail = smtpSettings["FromEmail"] ?? throw new InvalidOperationException("SMTP FromEmail is not configured");
                var fromName = smtpSettings["FromName"] ?? "Coffee School";
                var smtpServer = smtpSettings["Server"] ?? throw new InvalidOperationException("SMTP Server is not configured");
                var smtpPort = int.Parse(smtpSettings["Port"] ?? "587");
                var smtpUsername = smtpSettings["Username"] ?? throw new InvalidOperationException("SMTP Username is not configured");
                var smtpPassword = smtpSettings["Password"] ?? throw new InvalidOperationException("SMTP Password is not configured");
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = enableSsl;
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = "Welcome to Coffee School - Admission Confirmed!",
                        IsBodyHtml = true,
                        Body = GetAdmissionConfirmationEmailBody(student)
                    };

                    mailMessage.To.Add(toEmail);

                    // Attach receipt if provided
                    if (!string.IsNullOrEmpty(receiptPath) && File.Exists(receiptPath))
                    {
                        var attachment = new Attachment(receiptPath);
                        mailMessage.Attachments.Add(attachment);
                    }

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation("Admission confirmation email sent successfully to {Email}", toEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send admission confirmation email to {Email}", toEmail);
                throw;
            }
        }

        private string GetAdmissionConfirmationEmailBody(Student student)
        {
            var admissionDate = student.AdmissionDate?.ToString("MMMM dd, yyyy") ?? DateTime.UtcNow.ToString("MMMM dd, yyyy");
            var orientationDate = student.AdmissionDate?.AddDays(7).ToString("MMMM dd, yyyy") ?? DateTime.UtcNow.AddDays(7).ToString("MMMM dd, yyyy");
            var classStartDate = student.AdmissionDate?.AddDays(14).ToString("MMMM dd, yyyy") ?? DateTime.UtcNow.AddDays(14).ToString("MMMM dd, yyyy");

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Admission Confirmation - Coffee School</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
    <table role='presentation' style='width: 100%; border-collapse: collapse;'>
        <tr>
            <td style='padding: 40px 0; text-align: center; background: linear-gradient(135deg, #6B4423 0%, #8B6F47 100%);'>
                <h1 style='color: #ffffff; margin: 0; font-size: 32px;'>☕ Coffee School</h1>
                <p style='color: #ffffff; margin: 10px 0 0 0; font-size: 16px;'>Premium Coffee Education & Training</p>
            </td>
        </tr>
        <tr>
            <td style='padding: 0;'>
                <table role='presentation' style='width: 650px; margin: 20px auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
                    <tr>
                        <td style='padding: 40px 30px;'>
                            <div style='text-align: center; margin-bottom: 30px;'>
                                <h2 style='color: #6B4423; margin: 0 0 10px 0; font-size: 28px;'>🎉 Congratulations!</h2>
                                <p style='color: #666; font-size: 18px; margin: 0;'>Your admission has been confirmed</p>
                            </div>

                            <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;'>
                                Dear <strong style='color: #333;'>{student.Name}</strong>,
                            </p>
                            
                            <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;'>
                                We are delighted to welcome you to <strong>Coffee School</strong>! Your passion for coffee excellence has brought you to the right place. 
                                We are committed to providing you with world-class training and expertise in the art and science of coffee.
                            </p>

                            <div style='background: linear-gradient(135deg, #f8f8f8 0%, #e8e8e8 100%); padding: 25px; border-radius: 8px; margin: 30px 0; border-left: 4px solid #6B4423;'>
                                <h3 style='color: #6B4423; margin: 0 0 15px 0; font-size: 20px;'>📋 Your Admission Details</h3>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr>
                                        <td style='padding: 8px 0; color: #555; font-size: 15px;'><strong>Student ID:</strong></td>
                                        <td style='padding: 8px 0; color: #333; font-size: 15px;'>{student.StudentId}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; color: #555; font-size: 15px;'><strong>Email:</strong></td>
                                        <td style='padding: 8px 0; color: #333; font-size: 15px;'>{student.Email}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; color: #555; font-size: 15px;'><strong>Phone:</strong></td>
                                        <td style='padding: 8px 0; color: #333; font-size: 15px;'>{student.Phone}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; color: #555; font-size: 15px;'><strong>Admission Date:</strong></td>
                                        <td style='padding: 8px 0; color: #333; font-size: 15px;'>{admissionDate}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; color: #555; font-size: 15px;'><strong>Status:</strong></td>
                                        <td style='padding: 8px 0;'><span style='background-color: #4caf50; color: white; padding: 4px 12px; border-radius: 12px; font-size: 13px; font-weight: bold;'>{student.Status}</span></td>
                                    </tr>
                                    {(string.IsNullOrEmpty(student.ReceiptNumber) ? "" : $@"
                                    <tr>
                                        <td style='padding: 8px 0; color: #555; font-size: 15px;'><strong>Receipt Number:</strong></td>
                                        <td style='padding: 8px 0; color: #6B4423; font-size: 15px; font-weight: bold;'>{student.ReceiptNumber}</td>
                                    </tr>")}
                                </table>
                            </div>

                            <div style='background-color: #fff8e1; padding: 20px; border-radius: 8px; margin: 30px 0; border-left: 4px solid #ffc107;'>
                                <h3 style='color: #f57c00; margin: 0 0 15px 0; font-size: 18px;'>📅 Important Dates</h3>
                                <ul style='margin: 0; padding-left: 20px; color: #666;'>
                                    <li style='margin-bottom: 10px; font-size: 15px;'><strong>Orientation:</strong> {orientationDate}</li>
                                    <li style='margin-bottom: 10px; font-size: 15px;'><strong>Classes Begin:</strong> {classStartDate}</li>
                                    <li style='margin-bottom: 0; font-size: 15px;'><strong>Document Submission Deadline:</strong> {student.AdmissionDate?.AddDays(5).ToString("MMMM dd, yyyy") ?? DateTime.UtcNow.AddDays(5).ToString("MMMM dd, yyyy")}</li>
                                </ul>
                            </div>

                            {(student.FeesTotal > 0 ? $@"
                            <div style='background-color: #e3f2fd; padding: 20px; border-radius: 8px; margin: 30px 0; border-left: 4px solid #2196f3;'>
                                <h3 style='color: #1976d2; margin: 0 0 15px 0; font-size: 18px;'>💰 Fee Information</h3>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr>
                                        <td style='padding: 8px 0; color: #555; font-size: 15px;'>Total Course Fees:</td>
                                        <td style='padding: 8px 0; color: #333; font-size: 15px; text-align: right; font-weight: bold;'>${student.FeesTotal:N2}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; color: #555; font-size: 15px;'>Amount Paid:</td>
                                        <td style='padding: 8px 0; color: #4caf50; font-size: 15px; text-align: right; font-weight: bold;'>${student.FeesPaid:N2}</td>
                                    </tr>
                                    <tr style='border-top: 2px solid #2196f3;'>
                                        <td style='padding: 12px 0; color: #333; font-size: 16px; font-weight: bold;'>Balance Due:</td>
                                        <td style='padding: 12px 0; color: #d32f2f; font-size: 18px; text-align: right; font-weight: bold;'>${student.FeesTotal - student.FeesPaid:N2}</td>
                                    </tr>
                                </table>
                            </div>" : "")}

                            <div style='background-color: #f5f5f5; padding: 20px; border-radius: 8px; margin: 30px 0;'>
                                <h3 style='color: #6B4423; margin: 0 0 15px 0; font-size: 18px;'>📚 What to Bring on First Day</h3>
                                <ul style='margin: 0; padding-left: 20px; color: #666;'>
                                    <li style='margin-bottom: 8px; font-size: 14px;'>Valid photo ID</li>
                                    <li style='margin-bottom: 8px; font-size: 14px;'>Printed admission receipt</li>
                                    <li style='margin-bottom: 8px; font-size: 14px;'>Notebook and writing materials</li>
                                    <li style='margin-bottom: 8px; font-size: 14px;'>Comfortable clothes and closed-toe shoes</li>
                                    <li style='margin-bottom: 0; font-size: 14px;'>Your enthusiasm and passion for coffee!</li>
                                </ul>
                            </div>

                            <div style='background-color: #e8f5e9; padding: 20px; border-radius: 8px; margin: 30px 0; border-left: 4px solid #4caf50;'>
                                <h3 style='color: #2e7d32; margin: 0 0 10px 0; font-size: 18px;'>✨ What's Next?</h3>
                                <p style='color: #666; font-size: 15px; line-height: 1.6; margin: 0 0 10px 0;'>
                                    Check your email for login credentials to access our <strong>Student Portal</strong> where you can:
                                </p>
                                <ul style='margin: 0; padding-left: 20px; color: #666;'>
                                    <li style='margin-bottom: 8px; font-size: 14px;'>View your course schedule</li>
                                    <li style='margin-bottom: 8px; font-size: 14px;'>Access learning materials</li>
                                    <li style='margin-bottom: 8px; font-size: 14px;'>Track your progress</li>
                                    <li style='margin-bottom: 0; font-size: 14px;'>Connect with instructors</li>
                                </ul>
                            </div>

                            <div style='text-align: center; margin: 30px 0; padding: 20px; background-color: #fafafa; border-radius: 8px;'>
                                <p style='color: #666; font-size: 15px; margin: 0 0 15px 0;'>Need help or have questions?</p>
                                <p style='color: #666; font-size: 14px; margin: 0;'>
                                    📧 <a href='mailto:admissions@coffeeschool.com' style='color: #6B4423; text-decoration: none;'>admissions@coffeeschool.com</a><br>
                                    📞 +1 (555) 123-4567<br>
                                    🕒 Mon-Fri: 9:00 AM - 6:00 PM
                                </p>
                            </div>

                            <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 30px 0 0 0;'>
                                We look forward to seeing you soon and helping you master the craft of coffee!
                            </p>

                            <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 20px 0 0 0;'>
                                Warm regards,<br>
                                <strong style='color: #6B4423;'>The Coffee School Team</strong><br>
                                <em style='color: #999; font-size: 14px;'>Where Passion Meets Perfection</em>
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
        <tr>
            <td style='padding: 30px 20px; text-align: center; background-color: #f9f9f9;'>
                <p style='color: #999999; font-size: 12px; margin: 0 0 5px 0;'>
                    © {DateTime.UtcNow.Year} Coffee School. All rights reserved.
                </p>
                <p style='color: #999999; font-size: 11px; margin: 0;'>
                    This email was sent to {student.Email}. Please add us to your address book.
                </p>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}
