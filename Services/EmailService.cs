using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
        Task SendEmailWithAttachmentAsync(string toEmail, string subject, string htmlMessage, byte[] attachmentData, string attachmentName);
        Task SendWelcomeEmailAsync(string toEmail, string firstName, string lastName);
        Task SendSuspendedEmailAsync(string toEmail, string reason);
        Task SendActivatedEmailAsync(string toEmail);
        Task SendCongratulationsEmailAsync(string toEmail, string reward);
        Task SendReminderEmailAsync(string toEmail, string daysActive, string studyHours, string points);
        Task SendNotificationEmailAsync(string toEmail, string newBooks, string upcomingEvents);
        Task SendPlanDetailsEmailAsync(string toEmail, string planType, string validUntil, string seat);
        Task SendOtpEmailAsync(string toEmail, string studentName, string otp);
        Task SendForgotPasswordEmailAsync(string toEmail, string studentName, string resetLink);
        Task SendReceiptEmailAsync(string toEmail, string amountPaid, string planName, string validUntil);
        Task SendSeatAllocatedEmailAsync(string toEmail, string seatNumber, string zone, string timing);
        Task SendHolidayAnnouncementEmailAsync(string toEmail, string occasion, string date);
        Task SendHolidayCancelledEmailAsync(string toEmail, string occasion, string date);
        Task SendSeatReleasedEmailAsync(string toEmail, string seatNumber, string reason);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public EmailService(IConfiguration config, ILogger<EmailService> logger, IServiceProvider serviceProvider)
        {
            _config = config;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        private async Task<(string host, int port, string user, string pass, string fromName, string fromEmail)> GetSmtpConfigAsync()
        {
            var host = _config["EmailSettings:SmtpHost"];
            var portString = _config["EmailSettings:SmtpPort"];
            var user = _config["EmailSettings:SmtpUser"];
            var pass = _config["EmailSettings:SmtpPass"];
            var fromName = _config["EmailSettings:FromName"] ?? "Shresht Library";
            var fromEmail = _config["EmailSettings:FromEmail"];

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var dbHost = await context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "smtp_host");
                if (dbHost != null && !string.IsNullOrEmpty(dbHost.Value)) host = dbHost.Value;
                
                var dbPort = await context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "smtp_port");
                if (dbPort != null && !string.IsNullOrEmpty(dbPort.Value)) portString = dbPort.Value;
                
                var dbUser = await context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "smtp_user");
                if (dbUser != null && !string.IsNullOrEmpty(dbUser.Value)) user = dbUser.Value;
                
                var dbPass = await context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "smtp_pass");
                if (dbPass != null && !string.IsNullOrEmpty(dbPass.Value)) pass = dbPass.Value;
                
                var dbFromName = await context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "smtp_from_name");
                if (dbFromName != null && !string.IsNullOrEmpty(dbFromName.Value)) fromName = dbFromName.Value;
                
                var dbFromEmail = await context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "smtp_from_email");
                if (dbFromEmail != null && !string.IsNullOrEmpty(dbFromEmail.Value)) fromEmail = dbFromEmail.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve SMTP settings from database. Falling back to appsettings.");
            }

            if (string.IsNullOrEmpty(fromEmail))
            {
                fromEmail = user;
            }
            
            int port = 587;
            if (int.TryParse(portString, out int parsedPort)) port = parsedPort;
            
            return (host, port, user, pass, fromName, fromEmail);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var config = await GetSmtpConfigAsync();

            if (string.IsNullOrEmpty(config.host) || string.IsNullOrEmpty(config.user) || string.IsNullOrEmpty(config.pass) || config.user == "your-email@gmail.com")
            {
                _logger.LogWarning("Email not sent. SMTP not configured in appsettings.");
                return;
            }

            using var client = new SmtpClient(config.host, config.port)
            {
                Credentials = new NetworkCredential(config.user, config.pass),
                EnableSsl = true,
                Timeout = 10000 // 10 seconds timeout
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(config.fromEmail, config.fromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            try
            {
                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent to {ToEmail} with subject '{Subject}'.", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {ToEmail}", toEmail);
            }
        }

        public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string htmlMessage, byte[] attachmentData, string attachmentName)
        {
            var config = await GetSmtpConfigAsync();

            if (string.IsNullOrEmpty(config.host) || string.IsNullOrEmpty(config.user) || string.IsNullOrEmpty(config.pass) || config.user == "your-email@gmail.com")
            {
                _logger.LogWarning("Email not sent. SMTP not configured in appsettings.");
                return;
            }

            using var client = new SmtpClient(config.host, config.port)
            {
                Credentials = new NetworkCredential(config.user, config.pass),
                EnableSsl = true,
                Timeout = 10000 // 10 seconds timeout
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(config.fromEmail, config.fromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            if (attachmentData != null && attachmentData.Length > 0 && !string.IsNullOrEmpty(attachmentName))
            {
                var stream = new System.IO.MemoryStream(attachmentData);
                mailMessage.Attachments.Add(new Attachment(stream, attachmentName, "application/pdf"));
            }

            try
            {
                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email with attachment sent to {ToEmail} with subject '{Subject}'.", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {ToEmail}", toEmail);
            }
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string firstName, string lastName)
        {
            var subject = "Welcome to Shresht Library! 🎉";
            var stats = new System.Collections.Generic.Dictionary<string, string> {
                { "Registration Date", DateTime.UtcNow.ToString("dd MMM yyyy") }
            };
            var html = EmailTemplateBuilder.BuildTemplate(
                title: $"Welcome {firstName} {lastName}!",
                subtitle: "We are thrilled to have you join Shresht Library. Your journey to excellence starts here.",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/welcome.png",
                colorStart: "#3b82f6", // blue-500
                colorEnd: "#4f46e5",   // indigo-600
                highlight: "WELCOME",
                actionText: "Explore Your Dashboard",
                footer: "Let's make studying great!",
                stats: stats
            );
            await SendEmailAsync(toEmail, subject, html);
        }

        public async Task SendSuspendedEmailAsync(string toEmail, string reason)
        {
            var subject = "Action Required: Account Suspended ⚠️";
            var subtitleText = "Your library account has been suspended.";
            var stats = new System.Collections.Generic.Dictionary<string, string> {
                { "Reason", string.IsNullOrEmpty(reason) ? "Policy violation or unpaid dues" : reason },
                { "Date", DateTime.UtcNow.ToString("dd MMM yyyy") }
            };
                
            var html = EmailTemplateBuilder.BuildTemplate(
                title: "Account Suspended",
                subtitle: subtitleText,
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/suspended.png",
                colorStart: "#ef4444", // red-500
                colorEnd: "#e11d48",   // rose-600
                highlight: null,
                actionText: "Contact Admin",
                footer: "Please reach out to resolve this issue.",
                stats: stats
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

        public async Task SendCongratulationsEmailAsync(string toEmail, string reward)
        {
            var subject = "Congratulations! You've unlocked a reward 🎉";
            var html = EmailTemplateBuilder.BuildTemplate(
                title: "Congratulations!",
                subtitle: "Thank you for being with us, you have unlocked:",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/congratulations.png",
                colorStart: "#6366f1", // indigo-500
                colorEnd: "#9333ea",   // purple-600
                actionText: "Redeem Reward",
                footer: "Thanks for being awesome!",
                reward: reward
            );
            await SendEmailAsync(toEmail, subject, html);
        }

        public async Task SendReminderEmailAsync(string toEmail, string daysActive, string studyHours, string points)
        {
            var subject = "We miss you! ⏰";
            var stats = new System.Collections.Generic.Dictionary<string, string> {
                { "Days Active", daysActive },
                { "Study Hours", studyHours },
                { "Points", points }
            };
            var html = EmailTemplateBuilder.BuildTemplate(
                title: "We miss you ...!",
                subtitle: $"It has been {daysActive} days since we last saw you.",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/reminder.png",
                colorStart: "#3b82f6", // blue-500
                colorEnd: "#06b6d4",   // cyan-500
                actionText: "Come Back Now",
                footer: "We hope to see you soon!",
                stats: stats
            );
            await SendEmailAsync(toEmail, subject, html);
        }

        public async Task SendNotificationEmailAsync(string toEmail, string newBooks, string upcomingEvents)
        {
            var subject = "Here is a quick update 📋";
            var stats = new System.Collections.Generic.Dictionary<string, string> {
                { "New Books", newBooks },
                { "Upcoming Events", upcomingEvents }
            };
            var html = EmailTemplateBuilder.BuildTemplate(
                title: "Here is a quick update",
                subtitle: "Check out what's new in your library dashboard.",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/notification.png",
                colorStart: "#a855f7", // purple-500
                colorEnd: "#6366f1",   // indigo-500
                actionText: "View Dashboard",
                footer: "Stay updated with Shresht Library",
                stats: stats
            );
            await SendEmailAsync(toEmail, subject, html);
        }

        public async Task SendPlanDetailsEmailAsync(string toEmail, string planType, string validUntil, string seat)
        {
            var subject = "Your Subscription Details 📚";
            var stats = new System.Collections.Generic.Dictionary<string, string> {
                { "Plan Type", planType },
                { "Valid Until", validUntil },
                { "Seat", seat }
            };
            var html = EmailTemplateBuilder.BuildTemplate(
                title: "Your Premium Plan",
                subtitle: "Here are the details of your active membership:",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/plan_details.png",
                colorStart: "#34d399", // emerald-400
                colorEnd: "#14b8a6",   // teal-500
                actionText: "Manage Plan",
                footer: "Enjoy your premium benefits!",
                stats: stats
            );
            await SendEmailAsync(toEmail, subject, html);
        }

        public async Task SendOtpEmailAsync(string toEmail, string studentName, string otp)
        {
            var subject = "Your OTP Code 🔐";
            var html = EmailTemplateBuilder.BuildTemplate(
                title: $"Hello {studentName}, Verify Your Login",
                subtitle: "Use the following OTP to complete your sign in. Valid for 10 mins.",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/otp.png",
                colorStart: "#fb923c", // orange-400
                colorEnd: "#ef4444",   // red-500
                highlight: string.Join(" ", otp.ToCharArray()),
                actionText: "Verify Now",
                footer: "If you didn't request this, please ignore this email."
            );
            await SendEmailAsync(toEmail, subject, html);
        }

        public async Task SendForgotPasswordEmailAsync(string toEmail, string studentName, string resetLink)
        {
            // Extract the OTP from the end of the resetLink
            string otp = resetLink.Split('=').LastOrDefault() ?? "unknown";
            
            var subject = "Reset Your Password 🔑";
            var html = EmailTemplateBuilder.BuildTemplate(
                title: $"Hello {studentName}, Reset Password",
                subtitle: $"We received a request to reset your password. Your 6-digit Reset OTP is: <strong>{otp}</strong>. You can enter this OTP in the app, or click the button below.",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/forgot_password.png",
                colorStart: "#fb7185", // rose-400
                colorEnd: "#ec4899",   // pink-500
                actionText: "Reset via Web",
                actionUrl: resetLink,
                footer: "If you didn't request a reset, you can safely ignore this email."
            );
            await SendEmailAsync(toEmail, subject, html);
        }

        public async Task SendReceiptEmailAsync(string toEmail, string amountPaid, string planName, string validUntil)
        {
            var subject = "Payment Receipt & Plan Activated ✅";
            var stats = new System.Collections.Generic.Dictionary<string, string> {
                { "Amount Paid", amountPaid },
                { "Plan Name", planName },
                { "Valid Until", validUntil }
            };
            var html = EmailTemplateBuilder.BuildTemplate(
                title: "Plan Activated!",
                subtitle: "Your payment was successful and your premium plan is now active.",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/receipt.png",
                colorStart: "#10b981", // emerald-500
                colorEnd: "#16a34a",   // green-600
                actionText: "View Dashboard",
                footer: "Thank you for choosing Shresht Library!",
                stats: stats
            );
            await SendEmailAsync(toEmail, subject, html);
        }

        public async Task SendSeatAllocatedEmailAsync(string toEmail, string seatNumber, string zone, string timing)
        {
            var subject = "Your Seat is Ready! 🪑";
            var stats = new System.Collections.Generic.Dictionary<string, string> {
                { "Zone", zone },
                { "Timing", timing }
            };
            var html = EmailTemplateBuilder.BuildTemplate(
                title: "Seat Allocated",
                subtitle: "A desk has been assigned to you. Here are your seating details:",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/seat_allocated.png",
                colorStart: "#fbbf24", // amber-400
                colorEnd: "#f97316",   // orange-500
                highlight: seatNumber,
                actionText: null, // Removed button as per request
                footer: "Please ensure you follow the seating rules.",
                stats: stats
            );
            await SendEmailAsync(toEmail, subject, html);
        }

        public async Task SendHolidayAnnouncementEmailAsync(string toEmail, string occasion, string date)
        {
            var subject = "Notice: Library Holiday 🌴";
            var stats = new System.Collections.Generic.Dictionary<string, string> {
                { "Occasion", occasion },
                { "Date", date }
            };
            var html = EmailTemplateBuilder.BuildTemplate(
                title: "Holiday Notice",
                subtitle: "The library will remain closed on account of the upcoming public holiday.",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/holiday.png",
                colorStart: "#38bdf8", // sky-400
                colorEnd: "#3b82f6",   // blue-500
                actionText: null, // Removed button
                footer: "Plan your study schedule accordingly!",
                stats: stats
            );
            await SendEmailAsync(toEmail, subject, html);
        }

        public async Task SendHolidayCancelledEmailAsync(string toEmail, string occasion, string date)
        {
            var subject = "Update: Holiday Cancelled 📅";
            var stats = new System.Collections.Generic.Dictionary<string, string> {
                { "Occasion", occasion },
                { "Date", date }
            };
            var html = EmailTemplateBuilder.BuildTemplate(
                title: "Holiday Cancelled",
                subtitle: "The previously announced holiday has been cancelled. The library will remain OPEN on this day.",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/holiday.png",
                colorStart: "#38bdf8", // sky-400
                colorEnd: "#3b82f6",   // blue-500
                actionText: null,
                footer: "We look forward to seeing you at the library!",
                stats: stats
            );
            await SendEmailAsync(toEmail, subject, html);
        }

        public async Task SendSeatReleasedEmailAsync(string toEmail, string seatNumber, string reason)
        {
            var subject = "Seat Reassignment Notice 🪑";
            var stats = new System.Collections.Generic.Dictionary<string, string> {
                { "Seat Released", seatNumber },
                { "Reason", reason }
            };
            var html = EmailTemplateBuilder.BuildTemplate(
                title: "Seat Released",
                subtitle: "Your assigned seat has been released by the administrator. Please contact the front desk for reassignment or more details.",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/seat.png",
                colorStart: "#f59e0b", // amber-500
                colorEnd: "#d97706",   // amber-600
                actionText: null, 
                footer: "For queries, please contact the library administration.",
                stats: stats
            );
            await SendEmailAsync(toEmail, subject, html);
        }
    }

    public static class EmailTemplateBuilder
    {
        public static string BuildTemplate(
            string title, 
            string subtitle, 
            string imageUrl, 
            string colorStart, 
            string colorEnd, 
            string highlight = null, 
            string actionText = "View Dashboard", 
            string actionUrl = "https://shreshtlibrary.onrender.com",
            string footer = "Thanks for being awesome!",
            string reward = null,
            System.Collections.Generic.Dictionary<string, string> stats = null)
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

            string rewardHtml = "";
            if (!string.IsNullOrEmpty(reward))
            {
                rewardHtml = $@"
                <div style='background-color: rgba(34, 197, 94, 0.1); border: 1px solid rgba(34, 197, 94, 0.2); border-radius: 12px; padding: 12px; margin-bottom: 24px; display: flex; align-items: center; gap: 12px;'>
                    <div style='width: 32px; height: 32px; background-color: rgba(34, 197, 94, 0.2); border-radius: 50%; display: flex; align-items: center; justify-content: center; color: #16a34a; font-size: 14px; flex-shrink: 0; margin-right: 12px;'>🎉</div>
                    <div style='text-align: left;'>
                        <div style='font-size: 11px; font-weight: 600; color: #16a34a; text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 2px;'>Unlocked</div>
                        <div style='font-size: 13px; font-weight: bold; color: #0f172a;'>{reward}</div>
                    </div>
                </div>";
            }

            string statsHtml = "";
            if (stats != null && stats.Count > 0)
            {
                statsHtml = "<div style='width: 100%; margin-bottom: 24px; text-align: left;'>";
                foreach (var stat in stats)
                {
                    statsHtml += $@"
                        <div style='display: flex; align-items: center; justify-content: space-between; padding: 12px 0; border-bottom: 1px solid #e2e8f0;'>
                            <span style='font-size: 13px; color: #64748b; font-weight: 500;'>{stat.Key}</span>
                            <span style='font-weight: bold; font-size: 13px; color: #0f172a;'>{stat.Value}</span>
                        </div>";
                }
                statsHtml += "</div>";
            }

            string actionHtml = "";
            if (!string.IsNullOrEmpty(actionText))
            {
                actionHtml = $@"
                <a href='{actionUrl}' style='display: inline-block; padding: 14px 32px; background-color: {colorStart}; color: #ffffff; font-weight: bold; text-decoration: none; border-radius: 12px; font-size: 14px; text-align: center;'>
                    {actionText}
                </a>";
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
                {rewardHtml}
                {statsHtml}
                {actionHtml}
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
