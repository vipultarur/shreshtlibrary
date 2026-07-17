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

            // Send backup file via email to super_admin and sub_super_admin
            try
            {
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var targetAdmins = await dbContext.AccountsCustomusers
                    .Where(u => (u.Role == "super_admin" || u.Role == "sub_super_admin") && !string.IsNullOrEmpty(u.Email))
                    .Select(u => u.Email)
                    .ToListAsync(ct);

                if (targetAdmins.Count > 0)
                {
                    var fileBytes = await File.ReadAllBytesAsync(filePath, ct);
                    string subject = "Weekly System Backup 📦";
                    string htmlMessage = $@"
                        <h3>Weekly Database Backup</h3>
                        <p>Please find attached the latest system backup generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.</p>
                        <p>This email is sent automatically to all authorized library administrators.</p>";

                    foreach (var adminEmail in targetAdmins)
                    {
                        await emailService.SendEmailWithAttachmentAsync(adminEmail, subject, htmlMessage, fileBytes, $"{backupId}.json");
                    }
                    _logger.LogInformation($"Backup emailed successfully to {targetAdmins.Count} administrators.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to email the weekly backup to administrators.");
            }
        }
    }
}
