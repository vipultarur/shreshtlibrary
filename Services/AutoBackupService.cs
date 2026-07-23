using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebApplication1.Data;

namespace WebApplication1.Services
{
    public class AutoBackupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutoBackupService> _logger;

        public AutoBackupService(IServiceProvider serviceProvider, ILogger<AutoBackupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Auto Backup Service started. Backups will run every 7 days.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GenerateBackupAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while generating the weekly backup.");
                }

                // Wait for 7 days before running again
                await Task.Delay(TimeSpan.FromDays(7), stoppingToken);
            }
        }

        private async Task GenerateBackupAsync(CancellationToken ct)
        {
            _logger.LogInformation("Generating weekly automatic JSON backup...");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var backupId = $"auto_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var backupData = new
            {
                BackupId = backupId,
                GeneratedAt = DateTime.UtcNow,
                Type = "Automatic Weekly Backup",
                Admins = await dbContext.AccountsAdminusers.AsNoTracking().ToListAsync(ct),
                Students = await dbContext.StudentsStudentprofiles.AsNoTracking().ToListAsync(ct),
                Facilities = await dbContext.LibraryFacilities.AsNoTracking().ToListAsync(ct),
                Achievers = await dbContext.LibraryAchievers.AsNoTracking().ToListAsync(ct),
                Sliders = await dbContext.LibraryHomesliders.AsNoTracking().ToListAsync(ct),
                LibraryInfo = await dbContext.LibraryLibraryinfos.AsNoTracking().ToListAsync(ct),
                LibraryAppConfigs = await dbContext.LibraryAppconfigs.AsNoTracking().ToListAsync(ct),
                LibraryReviews = await dbContext.LibraryReviews.AsNoTracking().ToListAsync(ct)
            };

            var json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions { WriteIndented = true });
            
            // Save to a "Backups" folder in the project root
            var backupDir = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            var filePath = Path.Combine(backupDir, $"{backupId}.json");
            await File.WriteAllTextAsync(filePath, json, ct);

            _logger.LogInformation($"Weekly backup successfully saved to: {filePath}");

            // Send backup file via email to Library Email and Administrators
            try
            {
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var recipients = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 1. Library Email
                var libraryInfo = await dbContext.LibraryLibraryinfos.AsNoTracking().FirstOrDefaultAsync(ct);
                if (libraryInfo != null && !string.IsNullOrWhiteSpace(libraryInfo.Email))
                {
                    recipients.Add(libraryInfo.Email.Trim());
                }

                // 2. Custom User Admins
                var customUserAdmins = await dbContext.AccountsCustomusers
                    .AsNoTracking()
                    .Where(u => (u.Role == "super_admin" || u.Role == "sub_super_admin") && !string.IsNullOrEmpty(u.Email))
                    .Select(u => u.Email)
                    .ToListAsync(ct);

                foreach (var email in customUserAdmins)
                {
                    if (!string.IsNullOrWhiteSpace(email)) recipients.Add(email.Trim());
                }

                // 3. Admin Users
                var adminUserEmails = await dbContext.AccountsAdminusers
                    .AsNoTracking()
                    .Where(u => !string.IsNullOrEmpty(u.Email))
                    .Select(u => u.Email!)
                    .ToListAsync(ct);

                foreach (var email in adminUserEmails)
                {
                    if (!string.IsNullOrWhiteSpace(email)) recipients.Add(email.Trim());
                }

                if (recipients.Count > 0)
                {
                    var fileBytes = await File.ReadAllBytesAsync(filePath, ct);
                    string subject = $"Weekly System Backup 📦 - {libraryInfo?.LibraryName ?? "Shresht Library"}";
                    string htmlMessage = $@"
                        <h3>Weekly System & Database Backup</h3>
                        <p>Please find attached the latest system backup JSON file generated at <strong>{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</strong>.</p>
                        <p>This automated email is delivered weekly to the library contact email and authorized system administrators.</p>
                        <p><strong>Library Name:</strong> {libraryInfo?.LibraryName ?? "Shresht Library"}<br/>
                        <strong>Backup ID:</strong> {backupId}</p>";

                    foreach (var recipientEmail in recipients)
                    {
                        await emailService.SendEmailWithAttachmentAsync(recipientEmail, subject, htmlMessage, fileBytes, $"{backupId}.json");
                    }
                    _logger.LogInformation($"Weekly backup emailed successfully to {recipients.Count} recipient(s): {string.Join(", ", recipients)}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to email the weekly backup file.");
            }
        }
    }
}
