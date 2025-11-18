using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BulkyBook.Utility
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                // Get SMTP settings from configuration
                var smtpHost = _configuration["Smtp:Host"];
                var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");
                var smtpUsername = _configuration["Smtp:Username"];
                var smtpPassword = _configuration["Smtp:Password"];
                var smtpFromEmail = _configuration["Smtp:FromEmail"] ?? smtpUsername;
                var smtpFromName = _configuration["Smtp:FromName"] ?? "BulkyBook";
                var enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true");

                // Validate SMTP configuration
                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername))
                {
                    Console.WriteLine("SMTP is not configured. Email not sent.");
                    Console.WriteLine($"To: {email}, Subject: {subject}");
                    return;
                }

                // Create mail message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpFromEmail, smtpFromName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true,
                    Priority = MailPriority.Normal
                };

                mailMessage.To.Add(new MailAddress(email));

                // Configure SMTP client
                using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.EnableSsl = enableSsl;
                    smtpClient.Timeout = 30000; // 30 seconds

                    // Send email
                    await smtpClient.SendMailAsync(mailMessage);
                    Console.WriteLine($"Email sent successfully to {email}");
                }
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"SMTP Error sending email to {email}: {smtpEx.Message}");
                Console.WriteLine($"Status Code: {smtpEx.StatusCode}");
                Console.WriteLine($"Stack trace: {smtpEx.StackTrace}");
                // Don't throw - we don't want email failures to break the order process
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email to {email}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // Don't throw - we don't want email failures to break the order process
            }
        }
    }
}
