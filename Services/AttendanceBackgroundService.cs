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
                    await ProcessAutoCheckoutAsync(stoppingToken);
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
        /// and send notifications. This runs every minute and processes any missed past records.
        /// </summary>
        private async Task ProcessPendingToAbsentAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

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
            
            // Find ALL PENDING records
            var pendingRecords = await context.AttendanceAttendances
                .Where(a => a.Method == "PENDING" && !a.IsPresent)
                .ToListAsync(stoppingToken);

            if (!pendingRecords.Any()) return;

            var currentIst = _dateTimeProvider.IstNow;
            var nowUtc = _dateTimeProvider.UtcNow;
            bool changesMade = false;
            
            // OPTIMIZATION: Fetch holiday status for all unique dates at once instead of inside the loop (N+1 fix)
            var uniqueDates = pendingRecords.Select(r => r.Date).Distinct().ToList();
            var holidayDates = await context.AttendanceHolidays
                .Where(h => h.IsActive && uniqueDates.Contains(h.Date))
                .Select(h => h.Date)
                .ToListAsync(stoppingToken);
            var holidayDatesSet = new HashSet<DateOnly>(holidayDates);

            foreach (var record in pendingRecords)
            {
                // Is this record's date a holiday?
                var isHoliday = holidayDatesSet.Contains(record.Date);


                // Calculate the exact cutoff DateTime for this record's date
                DateTime cutoffDateTime;
                if (closeTime < openTime) // Wraps around midnight (e.g. 10:00 to 01:00)
                {
                    cutoffDateTime = record.Date.ToDateTime(closeTime).AddDays(1).AddMinutes(paddingMinutes);
                }
                else
                {
                    cutoffDateTime = record.Date.ToDateTime(closeTime).AddMinutes(paddingMinutes);
                }

                if (currentIst > cutoffDateTime || isHoliday)
                {
                    record.Method = "SYSTEM";
                    record.MarkedAt = nowUtc;
                    changesMade = true;
                    
                    context.CoreActivitylogs.Add(new CoreActivitylog
                    {
                        Action = "ATTENDANCE_UPDATE",
                        UserId = record.StudentId,
                        Timestamp = nowUtc,
                        Details = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            Student = record.StudentId,
                            AttendanceDate = record.Date.ToString("yyyy-MM-dd"),
                            PreviousStatus = "Pending",
                            NewStatus = "Absent",
                            Method = "SYSTEM",
                            AttendanceTime = (string)null,
                            UpdatedBy = "SYSTEM",
                            LateMark = false
                        })
                    });
                    
                    // Add Notification if it's not a holiday
                    if (!isHoliday)
                    {
                        var notification = new NotificationsNotification
                        {
                            Title = "Attendance Update",
                            Body = $"You have been marked Absent for {record.Date.ToString("dd MMM yyyy")} because you did not check in by the cutoff time.",
                            Type = "ATTENDANCE",
                            TargetGroup = "all",
                            Target = "ALL",
                            Audience = "selected",
                            DisplayMode = "persistent",
                            Layout = "text_only",
                            Subtitle = "",
                            Description = "",
                            LinkButtonText = "",
                            CreatedAt = nowUtc,
                            ScheduledAt = nowUtc,
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
                }
            }

            if (changesMade)
            {
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Auto-marked pending students as Absent.");
            }
        }

        /// <summary>
        /// Step 5: Once the attendance window closes, automatically check out any students who are still checked in.
        /// </summary>
        private async Task ProcessAutoCheckoutAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var libraryInfo = await context.LibraryLibraryinfos.AsNoTracking().Select(l => new { l.OpeningTime, l.ClosingTime }).FirstOrDefaultAsync(stoppingToken);
            var openTime = libraryInfo?.OpeningTime ?? new TimeOnly(10, 0);
            var closeTime = libraryInfo?.ClosingTime ?? new TimeOnly(22, 0);
            
            var currentIst = _dateTimeProvider.IstNow;

            // Find all checked-in records that haven't been checked out
            var activeRecords = await context.AttendanceAttendances
                .Where(a => a.IsPresent && a.TimeOut == null)
                .ToListAsync(stoppingToken);

            if (!activeRecords.Any()) return;

            var nowUtc = _dateTimeProvider.UtcNow;
            bool changesMade = false;
            
            foreach (var record in activeRecords)
            {
                // Calculate cutoff for auto-checkout
                DateTime cutoffDateTime;
                if (closeTime < openTime) // Wraps around midnight
                {
                    cutoffDateTime = record.Date.ToDateTime(closeTime).AddDays(1);
                }
                else
                {
                    cutoffDateTime = record.Date.ToDateTime(closeTime);
                }

                if (currentIst > cutoffDateTime)
                {
                    record.TimeOut = closeTime;
                    
                    var duration = closeTime.ToTimeSpan() - record.TimeIn.ToTimeSpan();
                    if (duration.TotalMinutes < 0) duration = duration.Add(TimeSpan.FromHours(24));
                    record.TotalHours = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}";
                    
                    changesMade = true;
                    
                    context.CoreActivitylogs.Add(new CoreActivitylog
                    {
                        Action = "ATTENDANCE_AUTO_CHECKOUT",
                        UserId = record.StudentId,
                        Timestamp = nowUtc,
                        Details = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            Student = record.StudentId,
                            AttendanceDate = record.Date.ToString("yyyy-MM-dd"),
                            CheckoutTime = closeTime.ToString("HH:mm:ss"),
                            TotalHours = record.TotalHours,
                            UpdatedBy = "SYSTEM"
                        })
                    });

                    // Close active study session if any
                    var activeSession = await context.StudyStudysessions
                        .FirstOrDefaultAsync(s => s.StudentId == record.StudentId && (s.Status == "active" || s.Status == "paused"), stoppingToken);

                    if (activeSession != null)
                    {
                        activeSession.Status = "completed";
                        activeSession.EndTime = cutoffDateTime;
                        var sessionDuration = cutoffDateTime - activeSession.StartTime;
                        activeSession.DurationMinutes = (int)Math.Max(0, sessionDuration.TotalMinutes - activeSession.PausedMinutes);

                        context.CoreActivitylogs.Add(new CoreActivitylog
                        {
                            Action = "STUDY_SESSION_AUTO_CLOSED",
                            UserId = record.StudentId,
                            Timestamp = nowUtc,
                            Details = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                SessionId = activeSession.Id,
                                Duration = activeSession.DurationMinutes
                            })
                        });
                    }
                }
            }

            if (changesMade)
            {
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Auto-checked out students.");
            }
        }
    }
}

