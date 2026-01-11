using System.Net;
using System.Net.Mail;

namespace JWTAuthAPI.Services
{
    public interface IEmailService
    {
        Task SendInquiryConfirmationEmailAsync(string toEmail, string fullName);
        Task SendOTPEmailAsync(string toEmail, string fullName, string otp);
        Task SendAccountActivationEmailAsync(string toEmail, string fullName, string email);
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
                        Subject = "Your Staff Account Verification Code",
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
            <h1>Coffee School Staff Portal</h1>
        </div>
        <div class='content'>
            <h2>Welcome to Coffee School!</h2>
            <p>Dear {fullName},</p>
            <p>An administrator has created a staff account for you. Please use the verification code below to complete your account setup:</p>
            
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
                        Subject = "Your Staff Account is Now Active!",
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
            <h1>Coffee School Staff Portal</h1>
        </div>
        <div class='content'>
            <div class='success-box'>
                <div class='success-icon'>✓</div>
                <h2 style='color: #28a745; margin: 10px 0;'>Account Activated!</h2>
            </div>
            
            <p>Dear {fullName},</p>
            <p>Great news! Your staff account has been activated by the administrator. You can now log in to the Coffee School staff portal.</p>
            
            <div class='credentials-box'>
                <p style='margin: 5px 0;'><strong>Your Login Email:</strong></p>
                <p style='margin: 5px 0; color: #6F4E37; font-size: 18px;'>{email}</p>
            </div>
            
            <p>Use the email address above along with the password you set during verification to log in to the system.</p>
            
            <p><strong>What's next?</strong></p>
            <ul>
                <li>Log in to the staff portal</li>
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
    }
}
