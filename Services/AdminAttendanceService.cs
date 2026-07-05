using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.DTOs.Attendance;

namespace WebApplication1.Services
{
    public class AdminAttendanceService : IAdminAttendanceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public AdminAttendanceService(ApplicationDbContext context, IEmailService emailService, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _emailService = emailService;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<ServiceResult<object>> GetCurrentQrAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            
            var expiredQrs = await _context.AttendanceQrcodes
                .Where(q => q.IsActive && !q.IsExpired && q.ExpiresAt.HasValue && q.ExpiresAt.Value <= now)
                .ToListAsync(ct);
            foreach (var eq in expiredQrs)
            {
                eq.IsActive = false;
                eq.IsExpired = true;
            }
            if (expiredQrs.Any()) await _context.SaveChangesAsync(ct);

            var currentQr = await _context.AttendanceQrcodes
                .Where(q => !q.IsExpired && q.IsActive)
                .OrderByDescending(q => q.CreatedAt)
                .Select(q => new {
                    id = q.Id,
                    token = q.Token,
                    code = q.Code,
                    qr_hash = q.QrHash,
                    valid_date = q.ValidDate.ToString("yyyy-MM-dd"),
                    is_active = q.IsActive,
                    is_expired = q.IsExpired,
                    generation_method = q.GenerationMethod,
                    expiry_timestamp = q.ExpiryTimestamp,
                    expires_at = q.ExpiresAt,
                    created_at = q.CreatedAt
                })
                .FirstOrDefaultAsync(ct);
                
            return ServiceResult<object>.Ok(currentQr);
        }

        public async Task<ServiceResult<object>> GetQrHistoryAsync(int page, int pageSize, CancellationToken ct = default)
        {
            pageSize = System.Math.Clamp(pageSize, 1, 100);
            var query = _context.AttendanceQrcodes.AsNoTracking().OrderByDescending(q => q.ValidDate).ThenByDescending(q => q.CreatedAt);
            
            var totalCount = await query.CountAsync(ct);
            var totalPages = (int)System.Math.Ceiling((double)totalCount / pageSize);
            
            var history = await query.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(q => new {
                    id = q.Id,
                    token = q.Token,
                    code = q.Code,
                    qr_hash = q.QrHash,
                    valid_date = q.ValidDate.ToString("yyyy-MM-dd"),
                    is_active = q.IsActive,
                    is_expired = q.IsExpired,
                    generation_method = q.GenerationMethod,
                    expiry_timestamp = q.ExpiryTimestamp,
                    expires_at = q.ExpiresAt,
                    created_at = q.CreatedAt
                })
                .ToListAsync(ct);
                
            return ServiceResult<object>.Ok(new {
                count = totalCount, 
                total_pages = totalPages == 0 ? 1 : totalPages, 
                current_page = page, 
                next = (string?)null, 
                previous = (string?)null, 
                data = history 
            });
        }

        private DateTime CalculateQrExpiry(string? expiryDuration)
        {
            return expiryDuration switch
            {
                "7day" => DateTime.UtcNow.AddDays(7),
                "1month" => DateTime.UtcNow.AddDays(30),
                _ => DateTime.UtcNow.AddDays(1) // default: 1 day
            };
        }

        public async Task<ServiceResult<object>> GenerateQrAsync(string? expiryDuration, CancellationToken ct = default)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.IstNow);
            
            var existing = await _context.AttendanceQrcodes
                .Where(q => !q.IsExpired && q.IsActive)
                .ToListAsync(ct);
                
            foreach(var ex in existing) {
                ex.IsExpired = true;
                ex.IsActive = false;
            }

            var expiresAt = CalculateQrExpiry(expiryDuration);
                
            var qr = new AttendanceQrcode {
                Code = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                Token = Guid.NewGuid(),
                ValidDate = today,
                IsExpired = false,
                IsActive = true,
                GenerationMethod = "manual",
                CreatedAt = DateTime.UtcNow,
                ExpiryTimestamp = expiresAt,
                ExpiresAt = expiresAt,
                QrHash = Guid.NewGuid().ToString()
            };
            
            _context.AttendanceQrcodes.Add(qr);
            await _context.SaveChangesAsync(ct);
            
            return ServiceResult<object>.Ok(new {
                id = qr.Id,
                token = qr.Token,
                code = qr.Code,
                qr_hash = qr.QrHash,
                valid_date = qr.ValidDate.ToString("yyyy-MM-dd"),
                is_active = qr.IsActive,
                is_expired = qr.IsExpired,
                generation_method = qr.GenerationMethod,
                expiry_timestamp = qr.ExpiryTimestamp,
                expires_at = qr.ExpiresAt,
                created_at = qr.CreatedAt
            });
        }

        public async Task<ServiceResult<object>> RegenerateQrAsync(string? expiryDuration, CancellationToken ct = default)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.IstNow);
            
            var existing = await _context.AttendanceQrcodes
                .Where(q => !q.IsExpired && q.IsActive)
                .ToListAsync(ct);
                
            foreach(var ex in existing) {
                ex.IsExpired = true;
                ex.IsActive = false;
            }

            var expiresAt = CalculateQrExpiry(expiryDuration);
            
            var qr = new AttendanceQrcode {
                Code = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                Token = Guid.NewGuid(),
                ValidDate = today,
                IsExpired = false,
                IsActive = true,
                GenerationMethod = "manual_regen",
                CreatedAt = DateTime.UtcNow,
                ExpiryTimestamp = expiresAt,
                ExpiresAt = expiresAt,
                QrHash = Guid.NewGuid().ToString()
            };
            
            _context.AttendanceQrcodes.Add(qr);
            await _context.SaveChangesAsync(ct);
            
            return ServiceResult<object>.Ok(new {
                id = qr.Id,
                token = qr.Token,
                code = qr.Code,
                qr_hash = qr.QrHash,
                valid_date = qr.ValidDate.ToString("yyyy-MM-dd"),
                is_active = qr.IsActive,
                is_expired = qr.IsExpired,
                generation_method = qr.GenerationMethod,
                expiry_timestamp = qr.ExpiryTimestamp,
                expires_at = qr.ExpiresAt,
                created_at = qr.CreatedAt
            });
        }

        public async Task<ServiceResult<bool>> ExpireAllQrAsync(CancellationToken ct = default)
        {
            var existing = await _context.AttendanceQrcodes
                .Where(q => !q.IsExpired && q.IsActive)
                .ToListAsync(ct);
                
            foreach(var ex in existing) {
                ex.IsExpired = true;
                ex.IsActive = false;
            }
            await _context.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteQrAsync(long pk, CancellationToken ct = default)
        {
            var qr = await _context.AttendanceQrcodes.FindAsync(new object[] { pk }, ct);
            if (qr == null)
                return ServiceResult<bool>.NotFound("QR not found");

            // Batch-null the FK in a single SQL statement instead of loading entities
            await _context.AttendanceAttendances
                .Where(a => a.QrCodeId == qr.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.QrCodeId, (long?)null), ct);

            _context.AttendanceQrcodes.Remove(qr);
            await _context.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> ClearAllQrAsync(CancellationToken ct = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(ct);
                try
            {
                await _context.AttendanceAttendances.ExecuteDeleteAsync(ct);
                await _context.AttendanceQrcodes.ExecuteDeleteAsync(ct);
                await transaction.CommitAsync(ct);
                return ServiceResult<bool>.Ok(true);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
            });
        }

        public async Task<ServiceResult<object>> GetQrScansAsync(long pk, CancellationToken ct = default)
        {
            var scans = await _context.AttendanceAttendances
                .Where(a => a.QrCodeId == pk)
                .Include(a => a.Student)
                .OrderByDescending(a => a.MarkedAt)
                .Select(a => new {
                    id = a.Id,
                    student = a.StudentId,
                    student_name = a.Student != null ? $"{a.Student.FirstName} {a.Student.LastName}".Trim() : null,
                    date = a.Date.ToString("yyyy-MM-dd"),
                    time_in = a.MarkedAt.HasValue ? TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(a.MarkedAt.Value, _dateTimeProvider.IstTimeZone)) : a.TimeIn,
                    is_present = a.IsPresent,
                    method = a.Method,
                    marked_at = a.MarkedAt
                })
                .ToListAsync(ct);
            return ServiceResult<object>.Ok(scans);
        }

        public async Task<ServiceResult<object>> GetHolidaysAsync(string? fromDate, string? toDate, string? date, bool? isActive, CancellationToken ct = default)
        {
            var query = _context.AttendanceHolidays.AsQueryable();

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

            return ServiceResult<object>.Ok(holidays);
        }

        public async Task<ServiceResult<object>> GetAttendanceDailySummaryAsync(string? date, CancellationToken ct = default)
        {
            if (!DateOnly.TryParse(date, out var targetDate)) targetDate = DateOnly.FromDateTime(_dateTimeProvider.IstNow);

            var total = await _context.AccountsCustomusers.CountAsync(u => u.Role == WebApplication1.Utils.Constants.Roles.Student && u.IsActive, ct);
            var records = await _context.AttendanceAttendances.Where(a => a.Date == targetDate).ToListAsync(ct);
            
            var present = records.Count(a => a.IsPresent);
            var systemAbsent = records.Count(a => !a.IsPresent && a.Method != "PENDING");
            var pendingRecords = records.Count(a => !a.IsPresent && a.Method == "PENDING");
            var unaccounted = total - records.Count;
            if (unaccounted < 0) unaccounted = 0;

            var libraryInfo = await _context.LibraryLibraryinfos.AsNoTracking().Select(l => new { l.OpeningTime, l.ClosingTime }).FirstOrDefaultAsync(ct);
            var paddingSetting = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "ATTENDANCE_PADDING_MINUTES", ct);
            
            var openTime = libraryInfo?.OpeningTime ?? new TimeOnly(10, 0);
            var closeTime = libraryInfo?.ClosingTime ?? new TimeOnly(22, 0);
            int paddingMinutes = 60;
            if (paddingSetting != null && int.TryParse(paddingSetting.Value, out int parsedPadding))
            {
                paddingMinutes = parsedPadding;
            }
            
            var currentTime = TimeOnly.FromDateTime(_dateTimeProvider.IstNow);
            var todayDate = DateOnly.FromDateTime(_dateTimeProvider.IstNow);

            var todayCutoffDateTime = closeTime < openTime 
                ? todayDate.ToDateTime(closeTime).AddDays(1).AddMinutes(paddingMinutes)
                : todayDate.ToDateTime(closeTime).AddMinutes(paddingMinutes);

            bool isPastCutoff = targetDate < todayDate;
            if (targetDate == todayDate)
            {
                isPastCutoff = _dateTimeProvider.IstNow > todayCutoffDateTime;
            }

            int absent, pending;
            if (isPastCutoff)
            {
                absent = systemAbsent + pendingRecords + unaccounted;
                pending = 0;
            }
            else
            {
                absent = systemAbsent;
                pending = pendingRecords + unaccounted;
            }

            return ServiceResult<object>.Ok(new {
                date = targetDate.ToString("yyyy-MM-dd"),
                present = present,
                absent = absent,
                pending = pending,
                total = total
            });
        }

        public async Task<ServiceResult<object>> GetAttendanceAbsenteesAsync(string? date, CancellationToken ct = default)
        {
            if (!DateOnly.TryParse(date, out var targetDate)) targetDate = DateOnly.FromDateTime(_dateTimeProvider.IstNow.Date);

            var libraryInfo = await _context.LibraryLibraryinfos.AsNoTracking().Select(l => new { l.OpeningTime, l.ClosingTime }).FirstOrDefaultAsync(ct);
            var paddingSetting = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "ATTENDANCE_PADDING_MINUTES", ct);
            
            var openTime = libraryInfo?.OpeningTime ?? new TimeOnly(10, 0);
            var closeTime = libraryInfo?.ClosingTime ?? new TimeOnly(22, 0);
            int paddingMinutes = 60;
            if (paddingSetting != null && int.TryParse(paddingSetting.Value, out int parsedPadding))
            {
                paddingMinutes = parsedPadding;
            }
            
            var currentTime = TimeOnly.FromDateTime(_dateTimeProvider.IstNow);
            var todayDate = DateOnly.FromDateTime(_dateTimeProvider.IstNow);

            var todayCutoffDateTime = closeTime < openTime 
                ? todayDate.ToDateTime(closeTime).AddDays(1).AddMinutes(paddingMinutes)
                : todayDate.ToDateTime(closeTime).AddMinutes(paddingMinutes);

            bool isPastCutoff = targetDate < todayDate;
            if (targetDate == todayDate)
            {
                isPastCutoff = _dateTimeProvider.IstNow > todayCutoffDateTime;
            }

            var records = await _context.AttendanceAttendances.Where(a => a.Date == targetDate).ToListAsync(ct);
            var presentIds = records.Where(r => r.IsPresent).Select(r => r.StudentId).ToHashSet();
            var absentIds = records.Where(r => !r.IsPresent && r.Method != "PENDING").Select(r => r.StudentId).ToHashSet();
            var pendingIds = records.Where(r => !r.IsPresent && r.Method == "PENDING").Select(r => r.StudentId).ToHashSet();

            var allStudents = await _context.AccountsCustomusers
                .Where(u => u.Role == WebApplication1.Utils.Constants.Roles.Student && u.IsActive)
                .Select(u => new {
                    user_id = u.Id,
                    student_id = u.StudentsStudentprofile != null ? u.StudentsStudentprofile.StudentId : null,
                    first_name = u.FirstName,
                    last_name = u.LastName,
                    mobile = u.Mobile,
                    username = u.Username,
                    date_joined = u.DateJoined
                })
                .ToListAsync(ct);

            var result = new List<object>();
            foreach(var stu in allStudents)
            {
                var utcJoinDate = stu.date_joined.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(stu.date_joined, DateTimeKind.Utc) : stu.date_joined.ToUniversalTime();
                var istJoinDate = TimeZoneInfo.ConvertTimeFromUtc(utcJoinDate, _dateTimeProvider.IstTimeZone);
                if (DateOnly.FromDateTime(istJoinDate) > targetDate)
                {
                    result.Add(new { user_id = stu.user_id, student_id = stu.student_id, username = stu.username, first_name = stu.first_name, last_name = stu.last_name, mobile = stu.mobile, attendance_status = "not_joined" });
                    continue;
                }

                if (presentIds.Contains(stu.user_id)) continue;

                if (absentIds.Contains(stu.user_id)) {
                    result.Add(new { user_id = stu.user_id, student_id = stu.student_id, username = stu.username, first_name = stu.first_name, last_name = stu.last_name, mobile = stu.mobile, attendance_status = "absent" });
                } else if (pendingIds.Contains(stu.user_id) || !presentIds.Contains(stu.user_id)) {
                    // PENDING record or no record at all
                    var status = isPastCutoff ? "absent" : "pending";
                    result.Add(new { user_id = stu.user_id, student_id = stu.student_id, username = stu.username, first_name = stu.first_name, last_name = stu.last_name, mobile = stu.mobile, attendance_status = status });
                }
            }

            return ServiceResult<object>.Ok(result);
        }

        public async Task<ServiceResult<object>> GetAttendanceStreakAsync(CancellationToken ct = default)
        {
            var thirtyDaysAgo = DateOnly.FromDateTime(_dateTimeProvider.IstNow.Date.AddDays(-30));
            var attendances = await _context.AttendanceAttendances
                .AsNoTracking()
                .Where(a => a.Date >= thirtyDaysAgo && a.IsPresent)
                .Select(a => new { a.StudentId, a.Date })
                .OrderByDescending(a => a.Date)
                .ToListAsync(ct);

            var grouped = attendances.GroupBy(a => a.StudentId);
            var streaks = new List<object>();
            var studentIds = grouped.Select(g => g.Key).ToList();
            var students = await _context.AccountsCustomusers.Where(u => studentIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u, ct);

            foreach (var group in grouped)
            {
                var dates = group.Select(a => a.Date).Distinct().OrderByDescending(d => d).ToList();
                int streak = 0;
                if (dates.Count > 0)
                {
                    var lastPresent = dates[0];
                    streak = 1;
                    for (int i = 1; i < dates.Count; i++)
                    {
                        if (dates[i].DayNumber == lastPresent.DayNumber - streak) streak++;
                        else break;
                    }
                }

                if (streak >= 1 && students.TryGetValue(group.Key, out var stu))
                {
                    streaks.Add(new {
                        student = new { user_id = stu.Id, first_name = stu.FirstName, last_name = stu.LastName },
                        streak = streak
                    });
                }
            }

            var sortedStreaks = streaks.OrderByDescending(s => (int)((dynamic)s).streak).Take(20).ToList();
            return ServiceResult<object>.Ok(sortedStreaks);
        }

        public async Task<ServiceResult<object>> GetAttendanceListAsync(string? date, string? fromDate, string? toDate, int page = 1, int pageSize = 100, string nextTemplate = "", string prevTemplate = "", CancellationToken ct = default)
        {
            var query = _context.AttendanceAttendances.Include(a => a.Student).AsNoTracking();
            if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var exactDate)) query = query.Where(a => a.Date == exactDate);
            if (!string.IsNullOrEmpty(fromDate) && DateOnly.TryParse(fromDate, out var fDate)) query = query.Where(a => a.Date >= fDate);
            if (!string.IsNullOrEmpty(toDate) && DateOnly.TryParse(toDate, out var tDate)) query = query.Where(a => a.Date <= tDate);

            var totalCount = await query.CountAsync(ct);
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);

            var data = await query
                .OrderByDescending(a => a.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new {
                    id = a.Id,
                    date = a.Date.ToString("yyyy-MM-dd"),
                    student = a.StudentId,
                    student_name = a.Student != null ? a.Student.FirstName + " " + a.Student.LastName : null,
                    time_in = a.MarkedAt.HasValue ? TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(a.MarkedAt.Value, _dateTimeProvider.IstTimeZone)) : a.TimeIn,
                    is_present = a.IsPresent,
                    is_manual = a.IsManual,
                    method = a.Method ?? "UNKNOWN",
                    marked_at = a.MarkedAt,
                    note = a.Note
                }).ToListAsync(ct);
                
            return ServiceResult<object>.Ok(new {
                count = totalCount,
                total_pages = totalPages,
                current_page = page,
                next = page < totalPages ? nextTemplate.Replace("{P}", (page + 1).ToString()) : null,
                previous = page > 1 ? prevTemplate.Replace("{P}", (page - 1).ToString()) : null,
                data = data
            });
        }

        public async Task<ServiceResult<bool>> RecordManualAttendanceAsync(ManualAttendanceDto dto, CancellationToken ct = default)
        {
            if (dto.StudentId == null && string.IsNullOrEmpty(dto.StudentMobile))
                return ServiceResult<bool>.Fail("Student ID or Mobile is required");

            long? targetStudentId = dto.StudentId;

            if (targetStudentId == null && !string.IsNullOrEmpty(dto.StudentMobile))
            {
                var student = await _context.AccountsCustomusers.FirstOrDefaultAsync(u => u.Mobile == dto.StudentMobile && u.Role.ToLower() == "student", ct);
                if (student == null)
                    return ServiceResult<bool>.NotFound("Student not found with provided mobile");
                targetStudentId = student.Id;
            }

            var targetStudent = await _context.AccountsCustomusers.FirstOrDefaultAsync(u => u.Id == targetStudentId && u.Role.ToLower() == "student", ct);
            if (targetStudent == null)
                return ServiceResult<bool>.NotFound("Student not found");

            if (!DateOnly.TryParse(dto.Date, out var targetDate))
                targetDate = DateOnly.FromDateTime(_dateTimeProvider.IstNow.Date);

            var diffDays = (_dateTimeProvider.IstNow.Date - targetDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
            if (diffDays < 0 || diffDays > 2)
                return ServiceResult<bool>.Fail("Attendance can only be edited for today and the previous 2 days.");

            if (await _context.AttendanceHolidays.AnyAsync(h => h.Date == targetDate, ct))
                return ServiceResult<bool>.Fail("Cannot modify attendance on a declared holiday.");

            var istJoinDate = TimeZoneInfo.ConvertTimeFromUtc(targetStudent.DateJoined.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(targetStudent.DateJoined, DateTimeKind.Utc) : targetStudent.DateJoined.ToUniversalTime(), _dateTimeProvider.IstTimeZone);
            if (DateOnly.FromDateTime(istJoinDate) > targetDate)
                return ServiceResult<bool>.Fail("Attendance cannot be recorded before the student joined the library.");

            var isPresent = dto.IsPresent ?? true;

            var existingRecord = await _context.AttendanceAttendances
                .FirstOrDefaultAsync(a => a.StudentId == targetStudentId && a.Date == targetDate, ct);

            // Determine cutoff for LateMark
            var libraryInfo = await _context.LibraryLibraryinfos.AsNoTracking().Select(l => new { l.ClosingTime }).FirstOrDefaultAsync(ct);
            var paddingSetting = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "ATTENDANCE_PADDING_MINUTES", ct);
            var closeTime = libraryInfo?.ClosingTime ?? new TimeOnly(22, 0);
            int manualPadding = 60;
            if (paddingSetting != null && int.TryParse(paddingSetting.Value, out int mp)) manualPadding = mp;
            var manualCutoff = closeTime.AddMinutes(manualPadding);
            var manualCurrentTime = TimeOnly.FromDateTime(_dateTimeProvider.IstNow);

            if (existingRecord != null)
            {
                existingRecord.IsPresent = isPresent;
                existingRecord.IsManual = true;
                existingRecord.Method = "MANUAL";
                if (!string.IsNullOrEmpty(dto.Note)) existingRecord.Note = dto.Note;
                existingRecord.MarkedAt = DateTime.UtcNow;
                if (isPresent && existingRecord.TimeIn == default)
                {
                     existingRecord.TimeIn = TimeOnly.FromDateTime(_dateTimeProvider.IstNow);
                }
                // Mark as late arrival if admin marks present after cutoff
                var targetDateCutoff = targetDate.ToDateTime(closeTime).AddMinutes(manualPadding);
                if (isPresent && _dateTimeProvider.IstNow > targetDateCutoff)
                {
                    existingRecord.LateMark = true;
                }
                else
                {
                    existingRecord.LateMark = false;
                }
            }
            else
            {
                var newRecord = new AttendanceAttendance
                {
                    StudentId = targetStudentId ?? 0,
                    Date = targetDate,
                    TimeIn = isPresent ? TimeOnly.FromDateTime(_dateTimeProvider.IstNow) : new TimeOnly(0, 0),
                    IsManual = true,
                    IsPresent = isPresent,
                    Method = "MANUAL",
                    Note = dto.Note,
                    MarkedAt = DateTime.UtcNow,
                    LateMark = isPresent && _dateTimeProvider.IstNow > targetDate.ToDateTime(closeTime).AddMinutes(manualPadding)
                };
                _context.AttendanceAttendances.Add(newRecord);
            }
            
            _context.CoreActivitylogs.Add(new CoreActivitylog
            {
                Action = "ATTENDANCE_UPDATE",
                UserId = targetStudentId ?? 0,
                Timestamp = _dateTimeProvider.UtcNow,
                Details = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Student = targetStudentId.Value,
                    AttendanceDate = targetDate.ToString("yyyy-MM-dd"),
                    PreviousStatus = existingRecord?.Method == "PENDING" ? "Pending" : (existingRecord != null ? (existingRecord.IsPresent ? "Present" : "Absent") : "None"),
                    NewStatus = isPresent ? "Present" : "Absent",
                    Method = "MANUAL",
                    AttendanceTime = isPresent ? _dateTimeProvider.IstNow.ToString("HH:mm:ss") : null,
                    UpdatedBy = "ADMIN",
                    LateMark = isPresent && _dateTimeProvider.IstNow > targetDate.ToDateTime(closeTime).AddMinutes(manualPadding)
                })
            });

            await _context.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<object>> RecordManualBulkAttendanceAsync(List<ManualAttendanceDto> dtos, CancellationToken ct = default)
        {
            if (dtos == null || dtos.Count == 0) return ServiceResult<object>.Fail("No attendance data provided");

            var studentIds = dtos.Where(d => d.StudentId.HasValue).Select(d => d.StudentId ?? 0).ToList();
            var mobiles = dtos.Where(d => d.StudentId == null && !string.IsNullOrEmpty(d.StudentMobile)).Select(d => d.StudentMobile).ToList();

            var studentsById = await _context.AccountsCustomusers.Where(u => studentIds.Contains(u.Id) && u.Role.ToLower() == "student").ToListAsync(ct);
            var studentsByMobile = await _context.AccountsCustomusers.Where(u => mobiles.Contains(u.Mobile) && u.Role.ToLower() == "student").ToListAsync(ct);

            var allValidStudentIds = studentsById.Select(s => s.Id).Concat(studentsByMobile.Select(s => s.Id)).ToHashSet();
            
            var parsedDates = dtos.Select(d => DateOnly.TryParse(d.Date, out var date) ? date : DateOnly.FromDateTime(_dateTimeProvider.IstNow.Date)).Distinct().ToList();

            foreach(var date in parsedDates) {
                var diffDays = (_dateTimeProvider.IstNow.Date - date.ToDateTime(TimeOnly.MinValue)).TotalDays;
                if (diffDays < 0 || diffDays > 2) return ServiceResult<object>.Fail("Attendance can only be edited for today and the previous 2 days.");
            }

            if (await _context.AttendanceHolidays.AnyAsync(h => parsedDates.Contains(h.Date), ct))
                return ServiceResult<object>.Fail("Cannot modify attendance on a declared holiday.");

            var allTargetStudents = studentsById.Concat(studentsByMobile).DistinctBy(s => s.Id).ToList();
            var earliestAllowedDate = parsedDates.Min();

            foreach (var student in allTargetStudents)
            {
                var istJoinDate = TimeZoneInfo.ConvertTimeFromUtc(student.DateJoined.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(student.DateJoined, DateTimeKind.Utc) : student.DateJoined.ToUniversalTime(), _dateTimeProvider.IstTimeZone);
                if (DateOnly.FromDateTime(istJoinDate) > earliestAllowedDate)
                {
                    // Check if any specific requested date for this student is before they joined
                    var requestedDatesForStudent = dtos.Where(d => (d.StudentId != null && d.StudentId == student.Id) || (!string.IsNullOrEmpty(d.StudentMobile) && d.StudentMobile == student.Mobile)).Select(d => DateOnly.TryParse(d.Date, out var dt) ? dt : DateOnly.FromDateTime(_dateTimeProvider.IstNow.Date));
                    foreach (var reqDate in requestedDatesForStudent)
                    {
                        if (DateOnly.FromDateTime(istJoinDate) > reqDate)
                        {
                            return ServiceResult<object>.Fail($"Attendance cannot be recorded for {student.FirstName} before their join date.");
                        }
                    }
                }
            }

            var existingRecords = await _context.AttendanceAttendances
                .Where(a => allValidStudentIds.Contains(a.StudentId) && parsedDates.Contains(a.Date))
                .ToListAsync(ct);

            var libraryInfo = await _context.LibraryLibraryinfos.AsNoTracking().Select(l => new { l.ClosingTime }).FirstOrDefaultAsync(ct);
            var paddingSetting = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "ATTENDANCE_PADDING_MINUTES", ct);
            var closeTime = libraryInfo?.ClosingTime ?? new TimeOnly(22, 0);
            int manualPadding = 60;
            if (paddingSetting != null && int.TryParse(paddingSetting.Value, out int mp)) manualPadding = mp;
            var manualCutoff = closeTime.AddMinutes(manualPadding);

            foreach (var dto in dtos)
            {
                long? targetStudentId = dto.StudentId;
                if (targetStudentId == null && !string.IsNullOrEmpty(dto.StudentMobile))
                {
                    targetStudentId = studentsByMobile.FirstOrDefault(u => u.Mobile == dto.StudentMobile)?.Id;
                }

                if (targetStudentId == null || !allValidStudentIds.Contains(targetStudentId.Value)) continue;

                if (!DateOnly.TryParse(dto.Date, out var targetDate)) targetDate = DateOnly.FromDateTime(_dateTimeProvider.IstNow);
                var isPresent = dto.IsPresent ?? true;

                var existingRecord = existingRecords.FirstOrDefault(a => a.StudentId == targetStudentId && a.Date == targetDate);

                var targetDateCutoff = targetDate.ToDateTime(closeTime).AddMinutes(manualPadding);
                var isLate = isPresent && _dateTimeProvider.IstNow > targetDateCutoff;

                if (existingRecord != null)
                {
                    existingRecord.IsPresent = isPresent;
                    existingRecord.IsManual = true;
                    existingRecord.Method = "MANUAL";
                    if (!string.IsNullOrEmpty(dto.Note)) existingRecord.Note = dto.Note;
                    existingRecord.MarkedAt = DateTime.UtcNow;
                    if (isPresent && existingRecord.TimeIn == default) existingRecord.TimeIn = TimeOnly.FromDateTime(_dateTimeProvider.IstNow);
                    existingRecord.LateMark = isLate;
                }
                else
                {
                    var newRecord = new AttendanceAttendance
                    {
                        StudentId = targetStudentId.Value,
                        Date = targetDate,
                        TimeIn = isPresent ? TimeOnly.FromDateTime(_dateTimeProvider.IstNow) : new TimeOnly(0, 0),
                        IsManual = true,
                        IsPresent = isPresent,
                        Method = "MANUAL",
                        Note = dto.Note,
                        MarkedAt = DateTime.UtcNow,
                        LateMark = isLate
                    };
                    _context.AttendanceAttendances.Add(newRecord);
                    existingRecords.Add(newRecord);
                }

                _context.CoreActivitylogs.Add(new CoreActivitylog
                {
                    Action = "ATTENDANCE_UPDATE",
                    UserId = targetStudentId.Value,
                    Timestamp = _dateTimeProvider.UtcNow,
                    Details = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Student = targetStudentId.Value,
                        AttendanceDate = targetDate.ToString("yyyy-MM-dd"),
                        PreviousStatus = existingRecord?.Method == "PENDING" ? "Pending" : (existingRecord != null ? (existingRecord.IsPresent ? "Present" : "Absent") : "None"),
                        NewStatus = isPresent ? "Present" : "Absent",
                        Method = "MANUAL",
                        AttendanceTime = isPresent ? _dateTimeProvider.IstNow.ToString("HH:mm:ss") : null,
                        UpdatedBy = "ADMIN",
                        LateMark = isLate
                    })
                });
            }

            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(new { message = $"{dtos.Count} records processed." });
        }

        public async Task<ServiceResult<object>> GetHolidayDetailAsync(long pk, CancellationToken ct = default)
        {
            var holiday = await _context.AttendanceHolidays
                .AsNoTracking()
                .Where(h => h.Id == pk)
                .Select(h => new {
                    id = h.Id,
                    date = h.Date.ToString("yyyy-MM-dd"),
                    title = h.Title,
                    description = h.Description,
                    is_active = h.IsActive,
                    created_at = h.CreatedAt,
                    updated_at = h.UpdatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (holiday == null) return ServiceResult<object>.NotFound("Holiday not found");
            return ServiceResult<object>.Ok(holiday);
        }

        public async Task<ServiceResult<object>> GetAttendanceDetailAsync(long pk, CancellationToken ct = default)
        {
            var attendance = await _context.AttendanceAttendances
                .AsNoTracking()
                .Include(a => a.Student)
                .Where(a => a.Id == pk)
                .Select(a => new {
                    id = a.Id,
                    date = a.Date.ToString("yyyy-MM-dd"),
                    student = a.StudentId,
                    student_name = a.Student != null ? a.Student.FirstName + " " + a.Student.LastName : null,
                    time_in = a.MarkedAt.HasValue ? TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(a.MarkedAt.Value, _dateTimeProvider.IstTimeZone)) : a.TimeIn,
                    is_present = a.IsPresent,
                    is_manual = a.IsManual,
                    method = a.Method ?? "UNKNOWN",
                    marked_at = a.MarkedAt,
                    note = a.Note
                })
                .FirstOrDefaultAsync(ct);

            if (attendance == null) return ServiceResult<object>.NotFound("Attendance record not found");
            return ServiceResult<object>.Ok(attendance);
        }

        public async Task<ServiceResult<object>> CreateHolidayAsync(HolidayDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Date) || !DateOnly.TryParse(dto.Date, out var date))
            {
                return ServiceResult<object>.Fail("Invalid date format.");
            }

            if (date < DateOnly.FromDateTime(_dateTimeProvider.IstNow.Date))
                return ServiceResult<object>.Fail("Cannot create holidays for past dates.");

            var holiday = new AttendanceHoliday
            {
                Date = date,
                Title = dto.Title ?? "Holiday",
                Description = dto.Description ?? "",
                IsActive = dto.IsActive ?? true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.AttendanceHolidays.Add(holiday);
            await _context.SaveChangesAsync(ct);

            // Fire and forget email notification to all active students
            _ = Task.Run(async () =>
            {
                using var scope = _context.Database.GetService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>().CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var activeStudents = await dbContext.AccountsCustomusers
                    .Where(u => u.Role == WebApplication1.Utils.Constants.Roles.Student && u.IsActive && !string.IsNullOrWhiteSpace(u.Email))
                    .Select(u => new { u.Email, Name = u.FirstName + " " + u.LastName })
                    .ToListAsync();

                foreach (var student in activeStudents)
                {
                    try
                    {
                        await emailService.SendHolidayAnnouncementEmailAsync(
                            student.Email ?? "",
                            holiday.Title,
                            holiday.Date.ToString("yyyy-MM-dd")
                        );
                    }
                    catch
                    {
                        // Ignore individual email failures to continue processing
                    }
                }
            });

            return ServiceResult<object>.Ok(new
            {
                id = holiday.Id,
                date = holiday.Date.ToString("yyyy-MM-dd"),
                title = holiday.Title,
                description = holiday.Description,
                is_active = holiday.IsActive,
                created_at = holiday.CreatedAt,
                updated_at = holiday.UpdatedAt
            });
        }

        public async Task<ServiceResult<object>> UpdateHolidayAsync(long pk, HolidayDto dto, CancellationToken ct = default)
        {
            var holiday = await _context.AttendanceHolidays.FindAsync(new object[] { pk }, ct);
            if (holiday == null) return ServiceResult<object>.NotFound("Holiday not found");

            if (holiday.Date < DateOnly.FromDateTime(_dateTimeProvider.IstNow.Date))
                return ServiceResult<object>.Fail("Past holidays cannot be edited.");

            if (!string.IsNullOrWhiteSpace(dto.Date) && DateOnly.TryParse(dto.Date, out var date))
            {
                holiday.Date = date;
            }
            if (dto.Title != null) holiday.Title = dto.Title;
            if (dto.Description != null) holiday.Description = dto.Description;
            if (dto.IsActive.HasValue) holiday.IsActive = dto.IsActive.Value;
            
            holiday.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok(new
            {
                id = holiday.Id,
                date = holiday.Date.ToString("yyyy-MM-dd"),
                title = holiday.Title,
                description = holiday.Description,
                is_active = holiday.IsActive,
                created_at = holiday.CreatedAt,
                updated_at = holiday.UpdatedAt
            });
        }

        public async Task<ServiceResult<bool>> DeleteHolidayAsync(long pk, CancellationToken ct = default)
        {
            var holiday = await _context.AttendanceHolidays.FindAsync(new object[] { pk }, ct);
            if (holiday == null) return ServiceResult<bool>.NotFound("Holiday not found");

            if (holiday.Date < DateOnly.FromDateTime(_dateTimeProvider.IstNow.Date))
                return ServiceResult<bool>.Fail("Past holidays cannot be deleted.");

            _context.AttendanceHolidays.Remove(holiday);
            await _context.SaveChangesAsync(ct);

            // Fire and forget email notification to all active students
            _ = Task.Run(async () =>
            {
                using var scope = _context.Database.GetService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>().CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var activeStudents = await dbContext.AccountsCustomusers
                    .Where(u => u.Role == WebApplication1.Utils.Constants.Roles.Student && u.IsActive && !string.IsNullOrWhiteSpace(u.Email))
                    .Select(u => new { u.Email })
                    .ToListAsync();

                foreach (var student in activeStudents)
                {
                    try
                    {
                        await emailService.SendHolidayCancelledEmailAsync(
                            student.Email ?? "",
                            holiday.Title,
                            holiday.Date.ToString("yyyy-MM-dd")
                        );
                    }
                    catch
                    {
                        // Ignore individual email failures to continue processing
                    }
                }
            });

            return ServiceResult<bool>.Ok(true);
        }
    }
}

