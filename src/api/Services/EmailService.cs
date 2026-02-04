using Habitera.Models;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Mail;
using System.Numerics;
using System.Threading.Tasks;


namespace Habitera.Services
{
    public interface IEmailService
    {
        Task SendEmailVerificationAsync(string email, string code);
        Task SendWelcomeEmailAsync(string email, string firstName);
        Task SendPasswordResetAsync(string email, string token);
        Task SendPasswordChangedNotificationAsync(string email, string firstName);
        Task SendPasswordResetConfirmationAsync(string email, string firstName);
        Task SendAccountDeletionConfirmationAsync(string email, string firstName);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendEmailVerificationAsync(string email, string code)
        {
            var subject = "Verify Your Email Address";
            var body = $@"
                <div style='font-family: Arial, sans-serif; line-height:1.6;'>
                    <h2 style='color:#2c3e50;'>Email Verification</h2>
                    <p>Thank you for registering with <strong>Habitera</strong>!</p>
                    <p>Please use the verification code below to confirm your account:</p>

                    <div style='margin:20px 0;'>
                        <span style='display:inline-block; padding:10px 20px; font-size:20px; 
                                     font-weight:bold; letter-spacing:3px; color:#fff; 
                                     background-color:#3498db; border-radius:6px;'>
                            {code}
                        </span>
                    </div>

                    <p>This code will expire in <strong>10 minutes</strong>.</p>
                    <p>If you did not create an account, you can safely ignore this email.</p>
                </div>
            ";

            await SendEmailViaBrevoAsync(email, subject, body);
        }

        public async Task SendPasswordResetAsync(string email, string token)
        {
            var subject = "Reset Your Password";
            var body = $@"
                <h2>Password Reset Request</h2>
                <p>We received a request to reset your password for your Habitera account.</p>
                <p>Please use the following code to reset your password:</p>
                <h1 style='color: #FF5722; font-size: 32px; letter-spacing: 5px;'>{token}</h1>
                <p>You can also reset your password by clicking this link: </p>
                <h1 style='color: #FF5722; font-size: 32px; letter-spacing: 5px;'>{token}</h1>
                <p>This code will expire in 10 minutes.</p>
                <p>If you didn't request a password reset, please ignore this email.</p>
            ";

            await SendEmailViaBrevoAsync(email, subject, body);
        }

        public async Task SendWelcomeEmailAsync(string email, string firstName)
        {
            var subject = "Welcome to Habitera!";
            var body = $@"
                <h2>Welcome to Habitera, {firstName}!</h2>
                <p>Thank you for verifying your email address. You're now ready to start building better habits!</p>
                <p>Here are some things you can do to get started:</p>
                <ul>
                    <li>Create your first habit</li>
                    <li>Set up daily reminders</li>
                    <li>Track your progress</li>
                    <li>Celebrate your achievements</li>
                </ul>
                <p>Happy habit building!</p>
                <p>The Habitera Team</p>
            ";

            await SendEmailViaBrevoAsync(email, subject, body);
        }

        public async Task SendPasswordChangedNotificationAsync(string email, string firstName)
        {
            var subject = "Your Habitera Password Was Changed";

            var body = $@"
        <div style='font-family: Arial, sans-serif; line-height:1.6;'>
            <h2 style='color:#2c3e50;'>Password Changed Successfully</h2>

            <p>Hi {firstName},</p>

            <p>This is a confirmation that your <strong>Habitera</strong> account password was changed.</p>

            <p>If you made this change, no further action is needed.</p>

            <p style='color:#c0392b;'>
                If you did <strong>not</strong> change your password, please reset it immediately and contact our support team.
            </p>

            <p>Staying secure,<br/>
            <strong>The Habitera Team</strong></p>
        </div>
    ";

            await SendEmailViaBrevoAsync(email, subject, body);
        }

        public async Task SendPasswordResetConfirmationAsync(string email, string firstName)
        {
            var subject = "Your Habitera Password Has Been Reset";

            var body = $@"
        <div style='font-family: Arial, sans-serif; line-height:1.6;'>
            <h2 style='color:#2c3e50;'>Password Reset Successful</h2>

            <p>Hi {firstName},</p>

            <p>Your password for your <strong>Habitera</strong> account has been successfully reset.</p>

            <p>You can now sign in using your new password.</p>

            <p style='color:#7f8c8d;'>
                If you did not request this reset, please contact our support team immediately.
            </p>

            <p>Welcome back!<br/>
            <strong>The Habitera Team</strong></p>
        </div>
    ";

            await SendEmailViaBrevoAsync(email, subject, body);
        }

        public async Task SendAccountDeletionConfirmationAsync(string email, string firstName)
        {
            var subject = "Your Habitera Account Has Been Deleted";

            var body = $@"
        <div style='font-family: Arial, sans-serif; line-height:1.6;'>
            <h2 style='color:#2c3e50;'>Account Deletion Confirmed</h2>

            <p>Hi {firstName},</p>

            <p>This email confirms that your <strong>Habitera</strong> account has been permanently deleted.</p>

            <p>
                All associated data, habits, and progress have been removed and can no longer be recovered.
            </p>

            <p style='color:#c0392b;'>
                If you did not request this action, please contact our support team immediately.
            </p>

            <p>
                Thank you for being part of Habitera. You're always welcome back if you decide to return.
            </p>

            <p>Take care,<br/>
            <strong>The Habitera Team</strong></p>
        </div>
    ";

            await SendEmailViaBrevoAsync(email, subject, body);
        }

        private async Task SendEmailViaBrevoAsync(string to, string subject, string body)
        {
            try
            {
                var smtpServer = _configuration["Brevo:SmtpServer"];
                var smtpPort = int.Parse(_configuration["Brevo:Port"] ?? "587");

                var fromEmail = _configuration["Brevo:FromEmail"];
                var fromName = _configuration["Brevo:FromName"] ?? "Habitera";
                var username = _configuration["Brevo:Username"];
                var password = _configuration["Brevo:Password"];

                if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("Brevo email configuration is missing. Required: FromEmail, Username, Password");
                    return;
                }

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(to);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent successfully via Brevo to: {to}");
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError($"SMTP error sending email to {to}: {smtpEx.StatusCode} - {smtpEx.Message}");
                throw new Exception("Failed to send email. Please check SMTP settings.");
            }
        }


    }
}