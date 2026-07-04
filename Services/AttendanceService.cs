using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.Extensions.Caching.Memory;

namespace WebApplication1.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public AttendanceService(ApplicationDbContext context, IDateTimeProvider dateTimeProvider, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _cache = cache;
        }

        public async Task<object?> GetTodayQrAsync(CancellationToken ct)
        {
            const string cacheKey = "TodayQr";
            if (_cache.TryGetValue(cacheKey, out object? cachedQr))
            {
                return cachedQr;
            }

            var nowUtc = DateTime.UtcNow;
            var qr = await _context.AttendanceQrcodes
                .AsNoTracking()
                .Where(q => q.IsActive && !q.IsExpired && q.ExpiresAt > nowUtc)
                .OrderByDescending(q => q.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (qr == null)
                return null;

            var result = new
            {
                id = qr.Id,
                code = qr.Code,
                qr_hash = qr.QrHash,
                valid_date = qr.ValidDate.ToString("yyyy-MM-dd"),
                is_active = qr.IsActive,
                is_expired = qr.IsExpired,
                expires_at = qr.ExpiresAt?.ToString("O")
            };
            
            _cache.Set(cacheKey, result, TimeSpan.FromSeconds(30));
            return result;
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

            var today = DateOnly.FromDateTime(_dateTimeProvider.IstNow);
            
            // Block scanning on holidays
            var isHoliday = await _context.AttendanceHolidays
                .AnyAsync(h => h.Date == today && h.IsActive, ct);
            if (isHoliday)
            {
                throw new InvalidOperationException("Attendance cannot be marked on a holiday.");
            }

            var libraryInfo = await _context.LibraryLibraryinfos.AsNoTracking().Select(l => new { l.OpeningTime, l.ClosingTime }).FirstOrDefaultAsync(ct);
            var paddingSetting = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "ATTENDANCE_PADDING_MINUTES", ct);
            
            var openTime = libraryInfo?.OpeningTime ?? new TimeOnly(10, 0);
            var closeTime = libraryInfo?.ClosingTime ?? new TimeOnly(22, 0);
            
            int paddingMinutes = 60;
            if (paddingSetting != null && int.TryParse(paddingSetting.Value, out int parsedPadding))
            {
                paddingMinutes = parsedPadding;
            }
            
            var startTime = openTime;
            var endTime = closeTime.AddMinutes(paddingMinutes);
            var currentTime = TimeOnly.FromDateTime(_dateTimeProvider.IstNow); // IST Time

            if (startTime <= endTime)
            {
                if (currentTime < startTime)
                {
                    throw new InvalidOperationException("Attendance window has not opened yet.");
                }

                if (currentTime > endTime)
                {
                    throw new InvalidOperationException("Attendance window has expired for today.");
                }
            }
            else
            {
                // Wraps around midnight
                if (currentTime > endTime && currentTime < startTime)
                {
                    throw new InvalidOperationException("Attendance window is closed.");
                }
            }

            var existing = await _context.AttendanceAttendances
                .FirstOrDefaultAsync(a => a.StudentId == userId && a.Date == today, ct);

            if (existing != null)
            {
                // If record is PENDING (created by midnight reset), update it to Present
                if (existing.Method == "PENDING" && !existing.IsPresent)
                {
                    existing.IsPresent = true;
                    existing.TimeIn = TimeOnly.FromDateTime(_dateTimeProvider.IstNow);
                    existing.QrCodeId = qr.Id;
                    existing.MarkedAt = _dateTimeProvider.UtcNow;
                    existing.Method = "QR";
                    existing.LateMark = currentTime > openTime.AddMinutes(paddingMinutes);
                    existing.UnderTime = false;

                    _context.CoreActivitylogs.Add(new CoreActivitylog
                    {
                        Action = "ATTENDANCE_UPDATE",
                        UserId = userId,
                        Timestamp = _dateTimeProvider.UtcNow,
                        Details = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            Student = userId,
                            AttendanceDate = today.ToString("yyyy-MM-dd"),
                            PreviousStatus = "Pending",
                            NewStatus = "Present",
                            Method = "QR",
                            AttendanceTime = _dateTimeProvider.IstNow.ToString("HH:mm:ss"),
                            UpdatedBy = "SYSTEM",
                            LateMark = currentTime > openTime.AddMinutes(paddingMinutes)
                        })
                    });

                    await _context.SaveChangesAsync(ct);

                    return new
                    {
                        id = existing.Id,
                        date = existing.Date.ToString("yyyy-MM-dd"),
                        time_in = existing.TimeIn.ToString("HH:mm:ss"),
                        is_present = existing.IsPresent,
                        method = existing.Method
                    };
                }

                throw new InvalidOperationException("Attendance already marked for today");
            }

            // No record exists (fallback if midnight reset hasn't run yet)
            var attendance = new WebApplication1.Models.AttendanceAttendance
            {
                Date = today,
                TimeIn = TimeOnly.FromDateTime(_dateTimeProvider.IstNow),
                IsManual = false,
                StudentId = userId,
                QrCodeId = qr.Id,
                IsPresent = true,
                MarkedAt = _dateTimeProvider.UtcNow,
                Method = "QR",
                LateMark = currentTime > openTime.AddMinutes(paddingMinutes),
                UnderTime = false
            };

            _context.AttendanceAttendances.Add(attendance);
            _context.CoreActivitylogs.Add(new CoreActivitylog
            {
                Action = "ATTENDANCE_UPDATE",
                UserId = userId,
                Timestamp = _dateTimeProvider.UtcNow,
                Details = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Student = userId,
                    AttendanceDate = today.ToString("yyyy-MM-dd"),
                    PreviousStatus = "Pending",
                    NewStatus = "Present",
                    Method = "QR",
                    AttendanceTime = _dateTimeProvider.IstNow.ToString("HH:mm:ss"),
                    UpdatedBy = "SYSTEM",
                    LateMark = currentTime > openTime.AddMinutes(paddingMinutes)
                })
            });

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

        public async Task<object> CheckoutAsync(long userId, CancellationToken ct)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.IstNow);
            var existing = await _context.AttendanceAttendances
                .FirstOrDefaultAsync(a => a.StudentId == userId && a.Date == today, ct);

            if (existing == null || !existing.IsPresent)
            {
                throw new InvalidOperationException("No active check-in found for today.");
            }

            if (existing.TimeOut != null)
            {
                throw new InvalidOperationException("Already checked out for today.");
            }

            var currentTime = TimeOnly.FromDateTime(_dateTimeProvider.IstNow);
            existing.TimeOut = currentTime;

            // Calculate total hours
            var duration = currentTime.ToTimeSpan() - existing.TimeIn.ToTimeSpan();
            existing.TotalHours = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}";

            _context.CoreActivitylogs.Add(new CoreActivitylog
            {
                Action = "ATTENDANCE_CHECKOUT",
                UserId = userId,
                Timestamp = _dateTimeProvider.UtcNow,
                Details = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Student = userId,
                    AttendanceDate = today.ToString("yyyy-MM-dd"),
                    CheckoutTime = currentTime.ToString("HH:mm:ss"),
                    TotalHours = existing.TotalHours
                })
            });

            // Close active study session if any
            var activeSession = await _context.StudyStudysessions
                .FirstOrDefaultAsync(s => s.StudentId == userId && (s.Status == "active" || s.Status == "paused"), ct);

            if (activeSession != null)
            {
                activeSession.Status = "completed";
                var endDt = _dateTimeProvider.IstNow;
                activeSession.EndTime = endDt;
                var sessionDuration = endDt - activeSession.StartTime;
                activeSession.DurationMinutes = (int)Math.Max(0, sessionDuration.TotalMinutes - activeSession.PausedMinutes);

                _context.CoreActivitylogs.Add(new CoreActivitylog
                {
                    Action = "STUDY_SESSION_AUTO_CLOSED",
                    UserId = userId,
                    Timestamp = _dateTimeProvider.UtcNow,
                    Details = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        SessionId = activeSession.Id,
                        Duration = activeSession.DurationMinutes
                    })
                });
            }

            await _context.SaveChangesAsync(ct);

            return new
            {
                message = "Checked out successfully."
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
                    time_in = a.MarkedAt.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(a.MarkedAt.Value, _dateTimeProvider.IstTimeZone).ToString("HH:mm:ss") : a.TimeIn.ToString("HH:mm:ss"),
                    time_out = a.TimeOut.HasValue ? a.TimeOut.Value.ToString("HH:mm:ss") : null,
                    is_present = a.IsPresent,
                    is_manual = a.IsManual,
                    method = a.Method,
                    note = a.Note,
                    late_mark = a.LateMark,
                    under_time = a.UnderTime,
                    total_hours = a.TotalHours,
                    status = a.Method == "PENDING" ? "Pending" : (!a.IsPresent ? "Absent" : (a.LateMark ? "Present (Arrived Late)" : "Present"))
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

