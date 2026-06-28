using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System;

namespace WebApplication1.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
        Task SendWelcomeEmailAsync(string toEmail, string firstName, string lastName);
        Task SendSuspendedEmailAsync(string toEmail, string reason);
        Task SendActivatedEmailAsync(string toEmail);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var host = _config["EmailSettings:SmtpHost"];
            var portString = _config["EmailSettings:SmtpPort"];
            var user = _config["EmailSettings:SmtpUser"];
            var pass = _config["EmailSettings:SmtpPass"];
            var fromName = _config["EmailSettings:FromName"] ?? "Shresht Library";
            var fromEmail = _config["EmailSettings:FromEmail"];

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) || user == "your-email@gmail.com")
            {
                // SMTP not configured, log and return
                Console.WriteLine("Email not sent. SMTP not configured in appsettings.");
                return;
            }

            int port = 587;
            if (int.TryParse(portString, out int parsedPort)) port = parsedPort;

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            try
            {
                await client.SendMailAsync(mailMessage);
                Console.WriteLine($"Email successfully sent to {toEmail} with subject '{subject}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email to {toEmail}: {ex.Message}");
            }
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string firstName, string lastName)
        {
            var subject = "Welcome to Shresht Library! 🎉";
            var html = EmailTemplateBuilder.BuildTemplate(
                title: $"Welcome {firstName} {lastName}!",
                subtitle: "We are thrilled to have you join Shresht Library. Your journey to excellence starts here.",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/welcome.png",
                colorStart: "#3b82f6", // blue-500
                colorEnd: "#4f46e5",   // indigo-600
                highlight: "WELCOME",
                actionText: "Explore Your Dashboard",
                footer: "Let's make studying great!"
            );
            await SendEmailAsync(toEmail, subject, html);
        }

        public async Task SendSuspendedEmailAsync(string toEmail, string reason)
        {
            var subject = "Action Required: Account Suspended ⚠️";
            var html = EmailTemplateBuilder.BuildTemplate(
                title: "Account Suspended",
                subtitle: "Your library account has been suspended due to a policy violation or unpaid dues.",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/suspended.png",
                colorStart: "#ef4444", // red-500
                colorEnd: "#e11d48",   // rose-600
                highlight: string.IsNullOrEmpty(reason) ? "SUSPENDED" : reason,
                actionText: "Contact Support",
                footer: "Please reach out to resolve this issue."
            );
            await SendEmailAsync(toEmail, subject, html);
        }

        public async Task SendActivatedEmailAsync(string toEmail)
        {
            var subject = "Your Subscription Details 📚";
            var html = EmailTemplateBuilder.BuildTemplate(
                title: "Account Reactivated!",
                subtitle: "Good news! Your Shresht Library account has been successfully reactivated. You can now access library facilities again.",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/congratulations.png",
                colorStart: "#10b981", // emerald-500
                colorEnd: "#059669",   // emerald-600
                actionText: "Go to Dashboard",
                footer: "We're glad to have you back!"
            );
            await SendEmailAsync(toEmail, subject, html);
        }
    }

    public static class EmailTemplateBuilder
    {
        public static string BuildTemplate(string title, string subtitle, string imageUrl, string colorStart, string colorEnd, string highlight = null, string actionText = "View Dashboard", string footer = "Thanks for being awesome!")
        {
            string highlightHtml = "";
            if (!string.IsNullOrEmpty(highlight))
            {
                highlightHtml = $@"
                <div style='background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 12px; margin-bottom: 24px; text-align: center;'>
                    <span style='font-size: 24px; font-family: monospace; font-weight: bold; letter-spacing: 0.4em; color: #0f172a;'>
                        {highlight}
                    </span>
                </div>";
            }

            return $@"
<!DOCTYPE html>
<html>
<body style='margin: 0; padding: 0; background-color: #f1f5f9; font-family: system-ui, -apple-system, sans-serif;'>
    <div style='padding: 40px 20px; display: flex; justify-content: center;'>
        <div style='max-width: 480px; width: 100%; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.05); border: 1px solid #e2e8f0;'>
            
            <!-- Top Gradient -->
            <div style='height: 8px; width: 100%; background: linear-gradient(to right, {colorStart}, {colorEnd});'></div>
            
            <!-- Header -->
            <div style='padding: 20px; text-align: center;'>
                <span style='background-color: #4f46e5; color: #ffffff; padding: 6px 8px; border-radius: 6px; font-weight: bold; font-size: 14px; margin-right: 8px;'>SL</span>
                <span style='font-weight: bold; font-size: 18px; color: #0f172a;'>ShreshtLibrary</span>
            </div>

            <!-- Content -->
            <div style='padding: 24px 32px; text-align: center;'>
                
                <div style='margin-bottom: 24px; display: flex; justify-content: center;'>
                    <img src='{imageUrl}' alt='Illustration' style='width: 120px; height: 120px; object-fit: contain; margin: 0 auto;' />
                </div>

                <h1 style='font-size: 24px; font-weight: bold; color: #0f172a; margin: 0 0 8px 0;'>{title}</h1>
                <p style='color: #64748b; font-size: 14px; line-height: 1.6; margin: 0 0 24px 0;'>
                    {subtitle}
                </p>

                {highlightHtml}

                <a href='https://shreshtlibrary.vercel.app' style='display: inline-block; width: 100%; padding: 14px 0; background: linear-gradient(to right, {colorStart}, {colorEnd}); color: #ffffff; font-weight: bold; text-decoration: none; border-radius: 12px; font-size: 14px;'>
                    {actionText}
                </a>
            </div>

            <!-- Footer -->
            <div style='background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 24px; text-align: center;'>
                <p style='color: #64748b; font-size: 14px; font-weight: 500; margin: 0 0 16px 0;'>
                    {footer}
                </p>
                <p style='color: #94a3b8; font-size: 12px; margin: 0;'>
                    If you would like to no longer receive updates, you may <a href='#' style='color: #94a3b8;'>unsubscribe</a>.
                </p>
            </div>
            
        </div>
    </div>
</body>
</html>";
        }
    }
}
