using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.Extensions.Logging;

namespace WebApplication1.Services
{
    public class AttendanceBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AttendanceBackgroundService> _logger;
        private readonly IDateTimeProvider _dateTimeProvider;
        private DateOnly _lastQrGeneratedDate = DateOnly.MinValue;

        public AttendanceBackgroundService(IServiceProvider serviceProvider, ILogger<AttendanceBackgroundService> logger, IDateTimeProvider dateTimeProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _dateTimeProvider = dateTimeProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Attendance Background Service is starting.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await AutoGenerateQrAsync(stoppingToken);
                    await ProcessPendingAttendanceAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in AttendanceBackgroundService.");
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task AutoGenerateQrAsync(CancellationToken stoppingToken)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.IstNow);
            if (_lastQrGeneratedDate >= today) return; // Already generated for today

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var nowUtc = DateTime.UtcNow;

            // 1. Expire any QRs that have naturally passed their ExpiresAt time
            var expiredQrs = await context.AttendanceQrcodes
                .Where(q => !q.IsExpired && q.IsActive && q.ExpiresAt <= nowUtc)
                .ToListAsync(stoppingToken);

            foreach (var ex in expiredQrs)
            {
                ex.IsExpired = true;
                ex.IsActive = false;
            }
            if (expiredQrs.Any())
            {
                await context.SaveChangesAsync(stoppingToken);
            }

            // 2. Check if a valid, unexpired QR still exists (e.g. a 1-month QR)
            var activeQrExists = await context.AttendanceQrcodes
                .AnyAsync(q => !q.IsExpired && q.IsActive && q.ExpiresAt > nowUtc, stoppingToken);

            if (activeQrExists)
            {
                _lastQrGeneratedDate = today;
                return; // We have a valid QR, no need to generate a new one
            }

            // Check if today is a holiday — don't generate QR on holidays
            var isHoliday = await context.AttendanceHolidays
                .AnyAsync(h => h.Date == today && h.IsActive, stoppingToken);

            if (isHoliday)
            {
                _lastQrGeneratedDate = today;
                _logger.LogInformation("Skipping QR auto-generation for {Date} — holiday.", today);
                return;
            }

            // Generate new QR for today with 1-day expiry
            var expiresAt = DateTime.UtcNow.AddDays(1);
            var qr = new AttendanceQrcode
            {
                Code = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                Token = Guid.NewGuid(),
                ValidDate = today,
                IsExpired = false,
                IsActive = true,
                GenerationMethod = "auto",
                CreatedAt = DateTime.UtcNow,
                ExpiryTimestamp = expiresAt,
                ExpiresAt = expiresAt,
                QrHash = Guid.NewGuid().ToString()
            };

            context.AttendanceQrcodes.Add(qr);
            await context.SaveChangesAsync(stoppingToken);

            _lastQrGeneratedDate = today;
            _logger.LogInformation("Auto-generated QR code for {Date}.", today);
        }

        private async Task ProcessPendingAttendanceAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var today = DateOnly.FromDateTime(_dateTimeProvider.IstNow);
            
            var holidayRecord = await context.AttendanceHolidays
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Date == today && h.IsActive, stoppingToken);

            if (holidayRecord != null) return; // No auto-absent on holidays

            var libraryInfo = await context.LibraryLibraryinfos.AsNoTracking().FirstOrDefaultAsync(stoppingToken);
            var paddingSetting = await context.CoreGlobalsettings
                .FirstOrDefaultAsync(s => s.Key == "ATTENDANCE_PADDING_MINUTES", stoppingToken);
            
            var openTime = libraryInfo?.OpenTime ?? new TimeOnly(10, 0);
            int paddingMinutes = 60;
            if (paddingSetting != null && int.TryParse(paddingSetting.Value, out int parsedPadding))
            {
                paddingMinutes = parsedPadding;
            }
            
            var cutoffTime = openTime.AddMinutes(paddingMinutes);
            var currentTime = TimeOnly.FromDateTime(_dateTimeProvider.IstNow); // IST Time

            if (currentTime <= cutoffTime) return; // Window is still open

            // Time has passed. Find all active students without an attendance record today.
            var activeStudentsWithoutAttendance = await context.AccountsCustomusers
                .Where(u => u.Role == "student" && u.IsActive && 
                            !context.AttendanceAttendances.Any(a => a.StudentId == u.Id && a.Date == today))
                .ToListAsync(stoppingToken);

            var validStudents = new System.Collections.Generic.List<AccountsCustomuser>();
            foreach (var student in activeStudentsWithoutAttendance)
            {
                var istJoinDate = TimeZoneInfo.ConvertTimeFromUtc(student.DateJoined.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(student.DateJoined, DateTimeKind.Utc) : student.DateJoined.ToUniversalTime(), _dateTimeProvider.IstTimeZone);
                if (DateOnly.FromDateTime(istJoinDate) <= today)
                {
                    validStudents.Add(student);
                }
            }

            if (!validStudents.Any()) return;

            _logger.LogInformation($"Auto-marking {validStudents.Count} students as Absent for {today}.");

            var now = _dateTimeProvider.UtcNow;
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            foreach (var student in validStudents)
            {
                var record = new AttendanceAttendance
                {
                    StudentId = student.Id,
                    Date = today,
                    IsPresent = false,
                    MarkedAt = now,
                    TimeIn = default,
                    LateMark = false,
                    IsManual = false,
                    Method = "SYSTEM"
                };
                context.AttendanceAttendances.Add(record);
                
                // Add Notification
                var notification = new NotificationsNotification
                {
                    Title = "Attendance Update",
                    Body = "You have been marked Absent for today because you did not check in by the cutoff time.",
                    Type = "ATTENDANCE",
                    TargetGroup = "all",
                    Target = "ALL",
                    Audience = "selected",
                    DisplayMode = "persistent",
                    Layout = "text_only",
                    Subtitle = "",
                    Description = "",
                    LinkButtonText = "",
                    CreatedAt = now,
                    ScheduledAt = now,
                    SendPush = true,
                    LinkUrl = "/attendance",
                    FailureCount = 0,
                    SuccessCount = 0,
                    TotalRecipients = 1
                };
                context.NotificationsNotifications.Add(notification);
                
                var studentNotification = new NotificationsStudentnotification
                {
                    StudentId = student.Id,
                    Notification = notification,
                    IsRead = false
                };
                context.NotificationsStudentnotifications.Add(studentNotification);
            }

            await context.SaveChangesAsync(stoppingToken);
        }
    }
}
