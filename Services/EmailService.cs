using System.Net;
using System.Net.Mail;

namespace JWTAuthAPI.Services
{
    public interface IEmailService
    {
        Task SendInquiryConfirmationEmailAsync(string toEmail, string fullName);
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
    }
}
