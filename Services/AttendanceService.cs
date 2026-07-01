using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public AttendanceService(ApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<object?> GetTodayQrAsync(CancellationToken ct)
        {
            var nowUtc = DateTime.UtcNow;
            var qr = await _context.AttendanceQrcodes
                .AsNoTracking()
                .Where(q => q.IsActive && !q.IsExpired && q.ExpiresAt > nowUtc)
                .OrderByDescending(q => q.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (qr == null)
                return null;

            return new
            {
                id = qr.Id,
                code = qr.Code,
                qr_hash = qr.QrHash,
                valid_date = qr.ValidDate.ToString("yyyy-MM-dd"),
                is_active = qr.IsActive,
                is_expired = qr.IsExpired,
                expires_at = qr.ExpiresAt?.ToString("O")
            };
        }

        public async Task<object?> ScanQrAsync(long userId, string qrHash, CancellationToken ct)
        {
            var nowUtc = DateTime.UtcNow;
            var qr = await _context.AttendanceQrcodes
                .FirstOrDefaultAsync(q => q.QrHash == qrHash && q.IsActive && !q.IsExpired && q.ExpiresAt > nowUtc, ct);

            if (qr == null)
            {
                throw new InvalidOperationException("Invalid or expired QR code");
            }

            var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
            
            // Block scanning on holidays
            var isHoliday = await _context.AttendanceHolidays
                .AnyAsync(h => h.Date == today && h.IsActive, ct);
            if (isHoliday)
            {
                throw new InvalidOperationException("Attendance cannot be marked on a holiday.");
            }

            var existing = await _context.AttendanceAttendances
                .FirstOrDefaultAsync(a => a.StudentId == userId && a.Date == today, ct);

            if (existing != null)
            {
                throw new InvalidOperationException("Attendance already marked for today");
            }

            var libraryInfo = await _context.LibraryLibraryinfos.AsNoTracking().FirstOrDefaultAsync(ct);
            var paddingSetting = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "ATTENDANCE_PADDING_MINUTES", ct);
            
            var openTime = libraryInfo?.OpenTime ?? new TimeOnly(10, 0);
            int paddingMinutes = 60;
            if (paddingSetting != null && int.TryParse(paddingSetting.Value, out int parsedPadding))
            {
                paddingMinutes = parsedPadding;
            }
            
            var cutoffTime = openTime.AddMinutes(paddingMinutes);
            var currentTime = TimeOnly.FromDateTime(_dateTimeProvider.IstNow); // IST Time

            if (currentTime > cutoffTime)
            {
                throw new InvalidOperationException("Attendance window has expired for today.");
            }

            var attendance = new WebApplication1.Models.AttendanceAttendance
            {
                Date = today,
                TimeIn = TimeOnly.FromDateTime(_dateTimeProvider.UtcNow),
                IsManual = false,
                StudentId = userId,
                QrCodeId = qr.Id,
                IsPresent = true,
                MarkedAt = _dateTimeProvider.UtcNow,
                Method = "QR",
                LateMark = false,
                UnderTime = false
            };

            _context.AttendanceAttendances.Add(attendance);
            await _context.SaveChangesAsync(ct);

            return new
            {
                id = attendance.Id,
                date = attendance.Date.ToString("yyyy-MM-dd"),
                time_in = attendance.TimeIn.ToString("HH:mm:ss"),
                is_present = attendance.IsPresent,
                method = attendance.Method
            };
        }

        public async Task<object> GetAttendanceLogsAsync(long userId, CancellationToken ct)
        {
            var logs = await _context.AttendanceAttendances
                .AsNoTracking()
                .Where(a => a.StudentId == userId)
                .OrderByDescending(a => a.Date)
                .Select(a => new
                {
                    id = a.Id,
                    date = a.Date.ToString("yyyy-MM-dd"),
                    time_in = a.TimeIn.ToString("HH:mm:ss"),
                    time_out = a.TimeOut.HasValue ? a.TimeOut.Value.ToString("HH:mm:ss") : null,
                    is_present = a.IsPresent,
                    is_manual = a.IsManual,
                    method = a.Method,
                    note = a.Note,
                    late_mark = a.LateMark,
                    under_time = a.UnderTime,
                    total_hours = a.TotalHours
                })
                .ToListAsync(ct);

            return logs;
        }

        public async Task<object> GetHolidaysAsync(string? fromDate, string? toDate, string? date, bool? isActive, CancellationToken ct)
        {
            var query = _context.AttendanceHolidays.AsNoTracking().AsQueryable();

            if (isActive.HasValue)
                query = query.Where(h => h.IsActive == isActive.Value);

            if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var exactDate))
                query = query.Where(h => h.Date == exactDate);

            if (!string.IsNullOrEmpty(fromDate) && DateOnly.TryParse(fromDate, out var fDate))
                query = query.Where(h => h.Date >= fDate);
            
            if (!string.IsNullOrEmpty(toDate) && DateOnly.TryParse(toDate, out var tDate))
                query = query.Where(h => h.Date <= tDate);

            var holidays = await query.OrderByDescending(h => h.Date).Select(h => new {
                id = h.Id,
                date = h.Date.ToString("yyyy-MM-dd"),
                title = h.Title,
                description = h.Description,
                is_active = h.IsActive,
                created_at = h.CreatedAt,
                updated_at = h.UpdatedAt
            }).ToListAsync(ct);

            return holidays;
        }
    }
}
