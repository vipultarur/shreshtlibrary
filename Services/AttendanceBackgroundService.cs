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
        private DateOnly _lastResetDate = DateOnly.MinValue;
        private DateOnly _lastAbsentMarkedDate = DateOnly.MinValue;

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
                    await MidnightResetAsync(stoppingToken);
                    await AutoGenerateQrAsync(stoppingToken);
                    await ProcessPendingToAbsentAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in AttendanceBackgroundService.");
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        /// <summary>
        /// Step 1: At 12:00 AM (or first run of the day), create PENDING attendance records for all active students.
        /// </summary>
        private async Task MidnightResetAsync(CancellationToken stoppingToken)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.IstNow);
            if (_lastResetDate >= today) return;

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var isHoliday = await context.AttendanceHolidays
                .AnyAsync(h => h.Date == today && h.IsActive, stoppingToken);
            if (isHoliday)
            {
                _lastResetDate = today;
                _logger.LogInformation("Skipping midnight reset for {Date} — holiday.", today);
                return;
            }

            var activeStudents = await context.AccountsCustomusers
                .Where(u => u.Role == "student" && u.IsActive)
                .ToListAsync(stoppingToken);

            var existingStudentIds = await context.AttendanceAttendances
                .Where(a => a.Date == today)
                .Select(a => a.StudentId)
                .ToListAsync(stoppingToken);
            var existingSet = new System.Collections.Generic.HashSet<long>(existingStudentIds);

            var now = _dateTimeProvider.UtcNow;
            int created = 0;

            foreach (var student in activeStudents)
            {
                if (existingSet.Contains(student.Id)) continue;

                var istJoinDate = TimeZoneInfo.ConvertTimeFromUtc(
                    student.DateJoined.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(student.DateJoined, DateTimeKind.Utc)
                        : student.DateJoined.ToUniversalTime(),
                    _dateTimeProvider.IstTimeZone);
                if (DateOnly.FromDateTime(istJoinDate) > today) continue;

                context.AttendanceAttendances.Add(new AttendanceAttendance
                {
                    StudentId = student.Id,
                    Date = today,
                    IsPresent = false,
                    MarkedAt = now,
                    TimeIn = default,
                    LateMark = false,
                    IsManual = false,
                    Method = "PENDING"
                });
                created++;
            }

            if (created > 0)
            {
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Midnight reset: Created {Count} pending attendance records for {Date}.", created, today);
            }

            _lastResetDate = today;
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

        /// <summary>
        /// Step 4: Once the attendance window closes, convert all PENDING records to Absent
        /// and send notifications. This runs once per day after cutoff.
        /// </summary>
        private async Task ProcessPendingToAbsentAsync(CancellationToken stoppingToken)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.IstNow);
            if (_lastAbsentMarkedDate >= today) return; // Already processed today

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var holidayRecord = await context.AttendanceHolidays
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Date == today && h.IsActive, stoppingToken);

            if (holidayRecord != null)
            {
                _lastAbsentMarkedDate = today;
                return;
            }

            var libraryInfo = await context.LibraryLibraryinfos.AsNoTracking().Select(l => new { l.OpeningTime, l.ClosingTime }).FirstOrDefaultAsync(stoppingToken);
            var paddingSetting = await context.CoreGlobalsettings
                .FirstOrDefaultAsync(s => s.Key == "ATTENDANCE_PADDING_MINUTES", stoppingToken);
            
            var openTime = libraryInfo?.OpeningTime ?? new TimeOnly(10, 0);
            var closeTime = libraryInfo?.ClosingTime ?? new TimeOnly(22, 0);
            int paddingMinutes = 60;
            if (paddingSetting != null && int.TryParse(paddingSetting.Value, out int parsedPadding))
            {
                paddingMinutes = parsedPadding;
            }
            
            var currentTime = TimeOnly.FromDateTime(_dateTimeProvider.IstNow);

            var startTime = openTime;
            var endTime = closeTime.AddMinutes(paddingMinutes);
            bool isPastCutoff = false;
            if (startTime <= endTime)
            {
                isPastCutoff = currentTime > endTime;
            }
            else
            {
                isPastCutoff = currentTime > endTime && currentTime < startTime;
            }

            if (!isPastCutoff) return; // Window is still open

            // Find all PENDING records for today and convert them to Absent
            var pendingRecords = await context.AttendanceAttendances
                .Where(a => a.Date == today && a.Method == "PENDING" && !a.IsPresent)
                .ToListAsync(stoppingToken);

            if (!pendingRecords.Any())
            {
                _lastAbsentMarkedDate = today;
                return;
            }

            _logger.LogInformation("Auto-marking {Count} pending students as Absent for {Date}.", pendingRecords.Count, today);

            var now = _dateTimeProvider.UtcNow;
            
            foreach (var record in pendingRecords)
            {
                record.Method = "SYSTEM";
                record.MarkedAt = now;
                
                context.CoreActivitylogs.Add(new CoreActivitylog
                {
                    Action = "ATTENDANCE_UPDATE",
                    UserId = record.StudentId,
                    Timestamp = now,
                    Details = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Student = record.StudentId,
                        AttendanceDate = today.ToString("yyyy-MM-dd"),
                        PreviousStatus = "Pending",
                        NewStatus = "Absent",
                        Method = "SYSTEM",
                        AttendanceTime = (string)null,
                        UpdatedBy = "SYSTEM",
                        LateMark = false
                    })
                });
                
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
                    StudentId = record.StudentId,
                    Notification = notification,
                    IsRead = false
                };
                context.NotificationsStudentnotifications.Add(studentNotification);
            }

            await context.SaveChangesAsync(stoppingToken);
            _lastAbsentMarkedDate = today;
        }
    }
}

