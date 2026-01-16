using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace IdealWeightNutrition.Utility
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
                var smtpFromName = _configuration["Smtp:FromName"] ?? "idealweightnutrition";
                var enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true");

                // Validate SMTP configuration
                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername))
                {
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
                }
            }
            catch (SmtpException smtpEx)
            {
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Send email with file attachment (for Excel reports, etc.)
        /// </summary>
        public async Task SendEmailWithAttachmentAsync(
            string email, 
            string subject, 
            string htmlMessage,
            byte[] attachmentBytes,
            string attachmentFileName)
        {
            try
            {
                // Get SMTP settings from configuration
                var smtpHost = _configuration["Smtp:Host"];
                var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");
                var smtpUsername = _configuration["Smtp:Username"];
                var smtpPassword = _configuration["Smtp:Password"];
                var smtpFromEmail = _configuration["Smtp:FromEmail"] ?? smtpUsername;
                var smtpFromName = _configuration["Smtp:FromName"] ?? "idealweightnutrition";
                var enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true");

                // Validate SMTP configuration
                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername))
                {
                    return;
                }

                // Create mail message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpFromEmail, smtpFromName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true,
                    Priority = MailPriority.High // High priority for admin alerts
                };

                mailMessage.To.Add(new MailAddress(email));

                // Add attachment
                if (attachmentBytes != null && attachmentBytes.Length > 0)
                {
                    var stream = new System.IO.MemoryStream(attachmentBytes);
                    // Determine MIME type based on file extension
                    string mimeType = "application/octet-stream";
                    if (attachmentFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        mimeType = "application/pdf";
                    }
                    else if (attachmentFileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || attachmentFileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                    {
                        mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    }
                    var attachment = new Attachment(stream, attachmentFileName, mimeType);
                    mailMessage.Attachments.Add(attachment);
                }

                // Configure SMTP client
                using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.EnableSsl = enableSsl;
                    smtpClient.Timeout = 60000; // 60 seconds (longer for attachments)

                    // Send email
                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (SmtpException smtpEx)
            {
            }
            catch (Exception ex)
            {
            }
        }
    }
}
