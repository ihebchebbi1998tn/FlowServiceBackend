using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace MyApi.Modules.Shared.Services
{
    public interface IForgotEmailService
    {
        Task<bool> SendPasswordResetEmailAsync(string recipientEmail, string resetLink, string recipientName = "User");
        Task<bool> SendOtpEmailAsync(string recipientEmail, string otpCode, string recipientName = "User");
    }

    /// <summary>
    /// Service for sending password reset emails via OVH SMTP
    /// Uses: testadminsupportgermararaza@spadadibattaglia.com
    /// SMTP: ssl0.ovh.net:465 (SSL)
    /// </summary>
    public class ForgotEmailService : IForgotEmailService
    {
        private readonly ILogger<ForgotEmailService> _logger;
        private readonly IConfiguration _configuration;

        // OVH SMTP Configuration
        private const string SMTP_HOST = "ssl0.ovh.net";
        private const int SMTP_PORT = 465;
        private const string SMTP_USERNAME = "testadminsupportgermararaza@spadadibattaglia.com";
        private const string SMTP_PASSWORD = "Dadouhibou2025";
        private const bool USE_SSL = true;

        public ForgotEmailService(
            ILogger<ForgotEmailService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Sends a password reset email with HTML template
        /// </summary>
        /// <param name="recipientEmail">Email address of the user requesting password reset</param>
        /// <param name="resetLink">Full URL link to reset password (e.g., https://localhost:3000/reset-password?token=xxx)</param>
        /// <param name="recipientName">Name of the user (optional, defaults to "User")</param>
        /// <returns>True if email sent successfully, false otherwise</returns>
        public async Task<bool> SendPasswordResetEmailAsync(string recipientEmail, string resetLink, string recipientName = "User")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recipientEmail) || string.IsNullOrWhiteSpace(resetLink))
                {
                    _logger.LogWarning("Invalid email or reset link provided for password reset");
                    return false;
                }

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("FlowService Support", SMTP_USERNAME));
                email.To.Add(new MailboxAddress(recipientName, recipientEmail));
                email.Subject = "Reset Your Password - FlowService";

                // Create HTML body
                var htmlBody = GeneratePasswordResetEmailHtml(recipientName, resetLink);

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody,
                    TextBody = $"Click the link to reset your password: {resetLink}"
                };

                email.Body = bodyBuilder.ToMessageBody();

                // Send via SMTP
                using (var client = new SmtpClient())
                {
                    // Connect with SSL
                    await client.ConnectAsync(SMTP_HOST, SMTP_PORT, USE_SSL);

                    // Authenticate
                    await client.AuthenticateAsync(SMTP_USERNAME, SMTP_PASSWORD);

                    // Send email
                    await client.SendAsync(email);

                    // Disconnect gracefully
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"Password reset email sent successfully to {recipientEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send password reset email to {recipientEmail}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generates professional HTML email template for password reset
        /// </summary>
        private string GeneratePasswordResetEmailHtml(string recipientName, string resetLink)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reset Your Password</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Helvetica Neue', sans-serif;
            background-color: #f5f5f5;
            color: #333;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 40px 20px;
            text-align: center;
            color: white;
        }}
        .header h1 {{
            font-size: 28px;
            margin-bottom: 5px;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .content h2 {{
            color: #333;
            font-size: 22px;
            margin-bottom: 20px;
        }}
        .content p {{
            color: #666;
            font-size: 16px;
            line-height: 1.6;
            margin-bottom: 20px;
        }}
        .reset-button {{
            display: inline-block;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 14px 40px;
            text-decoration: none;
            border-radius: 6px;
            font-weight: 600;
            margin: 30px 0;
            transition: transform 0.2s;
        }}
        .reset-button:hover {{
            transform: translateY(-2px);
        }}
        .warning {{
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
            font-size: 14px;
            color: #856404;
        }}
        .link-container {{
            background-color: #f8f9fa;
            padding: 15px;
            border-radius: 4px;
            margin: 20px 0;
            word-break: break-all;
            font-size: 12px;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 30px;
            text-align: center;
            color: #999;
            font-size: 12px;
            border-top: 1px solid #e0e0e0;
        }}
        .footer p {{
            margin: 5px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Password Reset</h1>
            <p>FlowService</p>
        </div>

        <div class='content'>
            <h2>Hello {recipientName},</h2>

            <p>We received a request to reset the password associated with your FlowService account. Click the button below to create a new password:</p>

            <center>
                <a href='{resetLink}' class='reset-button'>Reset Password</a>
            </center>

            <p style='text-align: center; color: #999; font-size: 14px;'>Or copy and paste this link in your browser:</p>
            <div class='link-container'>
                <a href='{resetLink}' style='color: #667eea; text-decoration: none;'>{resetLink}</a>
            </div>

            <div class='warning'>
                <strong>⏱️ Important:</strong> This link will expire in 1 hour for security reasons. If you didn't request a password reset, please ignore this email or <a href='' style='color: #856404; text-decoration: underline;'>contact support</a>.
            </div>

            <p style='font-size: 14px;'>
                For your security, never share this link with anyone. FlowService support will never ask you for your password.
            </p>
        </div>

        <div class='footer'>
            <p><strong>FlowService™</strong> | Professional Workflow Management</p>
            <p>© {DateTime.Now.Year} FlowService. All rights reserved.</p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Sends OTP code email for password reset verification
        /// </summary>
        /// <param name="recipientEmail">Email address to send OTP to</param>
        /// <param name="otpCode">6-digit OTP code</param>
        /// <param name="recipientName">Name of the user (optional)</param>
        /// <returns>True if email sent successfully, false otherwise</returns>
        public async Task<bool> SendOtpEmailAsync(string recipientEmail, string otpCode, string recipientName = "User")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recipientEmail) || string.IsNullOrWhiteSpace(otpCode))
                {
                    _logger.LogWarning("Invalid email or OTP provided for sending");
                    return false;
                }

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("FlowService Support", SMTP_USERNAME));
                email.To.Add(new MailboxAddress(recipientName, recipientEmail));
                email.Subject = "Your Password Reset Code - FlowService";

                // Create HTML body with OTP
                var htmlBody = GenerateOtpEmailHtml(recipientName, otpCode);

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody,
                    TextBody = $"Your OTP code is: {otpCode}. This code will expire in 5 minutes."
                };

                email.Body = bodyBuilder.ToMessageBody();

                // Send via SMTP
                using (var client = new SmtpClient())
                {
                    // Connect with SSL
                    await client.ConnectAsync(SMTP_HOST, SMTP_PORT, USE_SSL);

                    // Authenticate
                    await client.AuthenticateAsync(SMTP_USERNAME, SMTP_PASSWORD);

                    // Send email
                    await client.SendAsync(email);

                    // Disconnect gracefully
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"OTP email sent successfully to {recipientEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send OTP email to {recipientEmail}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generates professional HTML email template for OTP verification
        /// </summary>
        private string GenerateOtpEmailHtml(string recipientName, string otpCode)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Your Password Reset Code</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Helvetica Neue', sans-serif;
            background-color: #f5f5f5;
            color: #333;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 40px 20px;
            text-align: center;
            color: white;
        }}
        .header h1 {{
            font-size: 28px;
            margin-bottom: 5px;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .content h2 {{
            color: #333;
            font-size: 22px;
            margin-bottom: 20px;
        }}
        .content p {{
            color: #666;
            font-size: 16px;
            line-height: 1.6;
            margin-bottom: 20px;
        }}
        .otp-box {{
            background: #f8f9fa;
            border: 2px dashed #667eea;
            padding: 30px;
            border-radius: 8px;
            text-align: center;
            margin: 30px 0;
        }}
        .otp-code {{
            font-size: 48px;
            font-weight: bold;
            color: #667eea;
            letter-spacing: 8px;
            font-family: 'Courier New', monospace;
            margin: 20px 0;
        }}
        .otp-warning {{
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
            font-size: 14px;
            color: #856404;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 30px;
            text-align: center;
            color: #999;
            font-size: 12px;
            border-top: 1px solid #e0e0e0;
        }}
        .footer p {{
            margin: 5px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Password Reset</h1>
            <p>FlowService</p>
        </div>

        <div class='content'>
            <h2>Hello {recipientName},</h2>

            <p>You requested to reset your FlowService account password. Use the code below to proceed:</p>

            <div class='otp-box'>
                <p style='color: #999; font-size: 14px; margin-bottom: 10px;'>Your verification code</p>
                <div class='otp-code'>{otpCode}</div>
                <p style='color: #999; font-size: 12px;'>Enter this code on the password reset page</p>
            </div>

            <div class='otp-warning'>
                <strong>⏱️ Important:</strong> This code will expire in 5 minutes. Do not share this code with anyone.
            </div>

            <p style='font-size: 14px;'>
                If you didn't request a password reset, please ignore this email and your password will remain unchanged.
            </p>
        </div>

        <div class='footer'>
            <p><strong>FlowService™</strong> | Professional Workflow Management</p>
            <p>© {DateTime.Now.Year} FlowService. All rights reserved.</p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
