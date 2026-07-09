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
        private DateOnly _lastExpiryReminderDate = DateOnly.MinValue;
        private DateOnly _lastLeaderboardRewardDate = DateOnly.MinValue;

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
                    await ProcessPlanExpiryRemindersAsync(stoppingToken);
                    await ProcessLeaderboardRewardsAsync(stoppingToken);
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

            var libraryInfo = await context.LibraryLibraryinfos.AsNoTracking().OrderBy(l => l.Id).Select(l => new { l.OpeningTime, l.ClosingTime }).FirstOrDefaultAsync(stoppingToken);
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
                .Include(a => a.Student)
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
                DateTime cutoffDateTime = record.Date.ToDateTime(openTime).AddMinutes(paddingMinutes);

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
                        try
                        {
                            var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
                            string stName = record.Student?.FirstName ?? "Student";
                            string msg = $"❌ *Attendance Update*\n\nHi {stName},\nYou have been marked Absent for {record.Date:dd MMM yyyy} because you did not check in by the cutoff time.";
                            await dispatcher.SendToStudentAsync(
                                record.StudentId,
                                "Attendance Update",
                                $"You have been marked Absent for {record.Date:dd MMM yyyy} because you did not check in by the cutoff time.",
                                WebApplication1.Utils.NotificationTypes.Attendance,
                                whatsappMessage: msg);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send absent notification for student {Id}", record.StudentId);
                        }
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

            var libraryInfo = await context.LibraryLibraryinfos.AsNoTracking().OrderBy(l => l.Id).Select(l => new { l.OpeningTime, l.ClosingTime }).FirstOrDefaultAsync(stoppingToken);
            var openTime = libraryInfo?.OpeningTime ?? new TimeOnly(10, 0);
            var closeTime = libraryInfo?.ClosingTime ?? new TimeOnly(22, 0);
            
            var currentIst = _dateTimeProvider.IstNow;

            // Find all checked-in records that haven't been checked out
            var activeRecords = await context.AttendanceAttendances
                .Where(a => a.IsPresent && a.TimeOut == null)
                .ToListAsync(stoppingToken);

            if (!activeRecords.Any()) return;

            var recordsToCheckout = activeRecords.Where(record => {
                DateTime cutoffDateTime;
                if (closeTime < openTime)
                    cutoffDateTime = record.Date.ToDateTime(closeTime).AddDays(1);
                else
                    cutoffDateTime = record.Date.ToDateTime(closeTime);
                return currentIst > cutoffDateTime;
            }).ToList();

            if (!recordsToCheckout.Any()) return;

            var nowUtc = _dateTimeProvider.UtcNow;
            bool changesMade = false;
            
            var studentIds = recordsToCheckout.Select(r => r.StudentId).ToList();
            var activeSessions = await context.StudyStudysessions
                .Where(s => studentIds.Contains(s.StudentId) && (s.Status == "active" || s.Status == "paused"))
                .ToDictionaryAsync(s => s.StudentId, stoppingToken);
            
            foreach (var record in recordsToCheckout)
            {
                DateTime cutoffIst;
                if (closeTime < openTime)
                    cutoffIst = record.Date.ToDateTime(closeTime).AddDays(1);
                else
                    cutoffIst = record.Date.ToDateTime(closeTime);
                    
                DateTime cutoffUtc = TimeZoneInfo.ConvertTimeToUtc(cutoffIst, _dateTimeProvider.IstTimeZone);

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
                if (activeSessions.TryGetValue(record.StudentId, out var activeSession))
                {
                    activeSession.Status = "completed";
                    activeSession.EndTime = cutoffUtc;
                    var sessionDuration = cutoffUtc - activeSession.StartTime;
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

            if (changesMade)
            {
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Auto-checked out students.");
            }
        }

        private async Task ProcessPlanExpiryRemindersAsync(CancellationToken stoppingToken)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.IstNow);
            if (_lastExpiryReminderDate >= today) return; // Only run once a day

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var tomorrow = today.AddDays(1);
                var threeDays = today.AddDays(3);
                
                var expiringMemberships = await context.MembershipsMemberships
                    .Include(m => m.Student)
                    .Include(m => m.Plan)
                    .Where(m => m.Status == "active" && (m.EndDate == tomorrow || m.EndDate == threeDays || m.EndDate == today))
                    .ToListAsync(stoppingToken);

                int sentCount = 0;
                foreach (var m in expiringMemberships)
                {
                    if (m.Student != null)
                    {
                        string stName = m.Student.FirstName ?? "Student";
                        string planName = m.Plan?.Name ?? "Library Plan";
                        string endDate = m.EndDate.ToString("dd MMM yyyy");
                        
                        string daysActiveText = "";
                        string whatsappMsg = "";
                        
                        if (m.EndDate == threeDays)
                        {
                            daysActiveText = "3 Days Remaining";
                            whatsappMsg = $"⏰ *Plan Expiry Reminder*\n\nHi {stName},\nYour {planName} plan will expire in 3 days on {endDate}. Please renew soon to continue your studies without interruption!";
                        }
                        else if (m.EndDate == tomorrow)
                        {
                            daysActiveText = "1 Day Remaining";
                            whatsappMsg = $"⏰ *Plan Expiry Reminder*\n\nHi {stName},\nYour {planName} plan expires tomorrow ({endDate}). Please renew to continue your studies without interruption!";
                        }
                        else if (m.EndDate == today)
                        {
                            daysActiveText = "Expired Today";
                            whatsappMsg = $"⚠️ *Plan Expired*\n\nHi {stName},\nYour {planName} plan has expired today ({endDate}). Please renew immediately to regain access to the library!";
                        }

                        if (!string.IsNullOrWhiteSpace(m.Student.Email) && !string.IsNullOrEmpty(daysActiveText))
                        {
                            try
                            {
                                await emailService.SendReminderEmailAsync(
                                    m.Student.Email, 
                                    daysActive: daysActiveText, 
                                    studyHours: planName, 
                                    points: endDate
                                );
                                sentCount++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to send plan expiry reminder to {Email}", m.Student.Email);
                            }
                        }

                        if (!string.IsNullOrEmpty(whatsappMsg))
                        {
                            try
                            {
                                var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
                                string title = m.EndDate == today ? "Plan Expired ⚠️" : "Plan Expiry Reminder ⏰";
                                string body = m.EndDate == today 
                                    ? $"Your {planName} plan has expired today." 
                                    : $"Your {planName} plan will expire on {endDate}.";
                                
                                await dispatcher.SendToStudentAsync(
                                    m.Student.Id,
                                    title,
                                    body,
                                    WebApplication1.Utils.NotificationTypes.Expiry,
                                    whatsappMessage: whatsappMsg);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to send plan expiry notification to {Mobile}", m.Student.Mobile);
                            }
                        }
                    }
                }

                _lastExpiryReminderDate = today;
                if (sentCount > 0)
                {
                    _logger.LogInformation("Sent {Count} plan expiry reminders for {Date}.", sentCount, tomorrow);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing plan expiry reminders.");
            }
        }

        private async Task ProcessLeaderboardRewardsAsync(CancellationToken stoppingToken)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.IstNow);
            if (_lastLeaderboardRewardDate >= today) return;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var yesterday = today.AddDays(-1);
                
                // Get completed sessions for yesterday
                var query = context.StudyStudysessions
                    .Include(s => s.Student)
                    .ThenInclude(s => s.StudentsStudentprofile)
                    .Where(s => s.Status == "completed" && s.EndTime != null && 
                                DateOnly.FromDateTime(s.StartTime) == yesterday);

                var sessions = await query.ToListAsync(stoppingToken);

                var leaderboardData = sessions
                    .GroupBy(s => s.StudentId)
                    .Select(g => new
                    {
                        Student = g.Select(s => s.Student).FirstOrDefault(),
                        TotalHours = g.Sum(s => (s.EndTime!.Value - s.StartTime).TotalHours)
                    })
                    .OrderByDescending(x => x.TotalHours)
                    .Take(3)
                    .ToList();

                if (leaderboardData.Any())
                {
                    int rank = 1;
                    foreach (var item in leaderboardData)
                    {
                        if (item.Student != null)
                        {
                            try
                            {
                                string stName = item.Student.FirstName ?? "Student";
                                string hoursStr = item.TotalHours.ToString("0.0");
                                string emoji = rank == 1 ? "🥇" : rank == 2 ? "🥈" : "🥉";
                                
                                string msg = $"{emoji} *Congratulations!* {emoji}\n\nHi {stName},\nYou achieved Rank {rank} on yesterday's leaderboard with {hoursStr} hours of study time! Keep up the great work and stay dedicated! 📚💪";
                                
                                var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
                                await dispatcher.SendToStudentAsync(
                                    item.Student.Id,
                                    "Leaderboard Reward 🏆",
                                    $"You achieved Rank {rank} with {hoursStr} hours! Keep it up!",
                                    WebApplication1.Utils.NotificationTypes.General,
                                    whatsappMessage: msg);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to send leaderboard reward notification for student {Id}", item.Student.Id);
                            }
                        }
                        rank++;
                    }
                }

                _lastLeaderboardRewardDate = today;
                _logger.LogInformation("Processed leaderboard rewards for {Date}.", yesterday);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing leaderboard rewards.");
            }
        }
    }
}
