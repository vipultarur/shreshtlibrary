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

        public AttendanceBackgroundService(IServiceProvider serviceProvider, ILogger<AttendanceBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Attendance Background Service is starting.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingAttendanceAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in AttendanceBackgroundService.");
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessPendingAttendanceAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            
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
            var currentTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(5).AddMinutes(30)); // IST Time

            if (currentTime <= cutoffTime) return; // Window is still open

            // Time has passed. Find all active students without an attendance record today.
            var activeStudentsWithoutAttendance = await context.AccountsCustomusers
                .Where(u => u.Role == "student" && u.IsActive && 
                            !context.AttendanceAttendances.Any(a => a.StudentId == u.Id && a.Date == today))
                .ToListAsync(stoppingToken);

            if (!activeStudentsWithoutAttendance.Any()) return;

            _logger.LogInformation($"Auto-marking {activeStudentsWithoutAttendance.Count} students as Absent for {today}.");

            var now = DateTime.UtcNow;
            foreach (var student in activeStudentsWithoutAttendance)
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
            }

            await context.SaveChangesAsync(stoppingToken);
        }
    }
}
