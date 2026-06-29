using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.DTOs.Attendance;

namespace WebApplication1.Services
{
    public class AdminAttendanceService : IAdminAttendanceService
    {
        private readonly ApplicationDbContext _context;

        public AdminAttendanceService(ApplicationDbContext context)
        {
            _context = context;
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
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            
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
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            
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
            if (qr != null)
            {
                var relatedAttendances = _context.AttendanceAttendances.Where(a => a.QrCodeId == qr.Id);
                foreach (var attendance in relatedAttendances)
                {
                    attendance.QrCodeId = null;
                }

                _context.AttendanceQrcodes.Remove(qr);
                await _context.SaveChangesAsync(ct);
                return ServiceResult<bool>.Ok(true);
            }
            return ServiceResult<bool>.NotFound("QR not found");
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
                    time_in = a.TimeIn,
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
            if (!DateOnly.TryParse(date, out var targetDate)) targetDate = DateOnly.FromDateTime(DateTime.UtcNow);

            var total = await _context.AccountsCustomusers.CountAsync(u => u.Role == WebApplication1.Utils.Constants.Roles.Student && u.IsActive, ct);
            var records = await _context.AttendanceAttendances.Where(a => a.Date == targetDate).ToListAsync(ct);
            
            var present = records.Count(a => a.IsPresent);
            var explicitlyAbsent = records.Count(a => !a.IsPresent);
            var pending = total - (present + explicitlyAbsent);
            if (pending < 0) pending = 0;

            return ServiceResult<object>.Ok(new {
                date = targetDate.ToString("yyyy-MM-dd"),
                present = present,
                absent = explicitlyAbsent,
                pending = pending,
                total = total
            });
        }

        public async Task<ServiceResult<object>> GetAttendanceAbsenteesAsync(string? date, CancellationToken ct = default)
        {
            if (!DateOnly.TryParse(date, out var targetDate)) targetDate = DateOnly.FromDateTime(DateTime.UtcNow);

            var records = await _context.AttendanceAttendances.Where(a => a.Date == targetDate).ToListAsync(ct);
            var presentIds = records.Where(r => r.IsPresent).Select(r => r.StudentId).ToHashSet();
            var absentIds = records.Where(r => !r.IsPresent).Select(r => r.StudentId).ToHashSet();

            var allStudents = await _context.AccountsCustomusers
                .Where(u => u.Role == WebApplication1.Utils.Constants.Roles.Student && u.IsActive)
                .Select(u => new {
                    user_id = u.Id,
                    student_id = u.StudentsStudentprofile != null ? u.StudentsStudentprofile.StudentId : null,
                    first_name = u.FirstName,
                    last_name = u.LastName,
                    mobile = u.Mobile,
                    username = u.Username
                })
                .ToListAsync(ct);

            var result = new List<object>();
            foreach(var stu in allStudents)
            {
                if (presentIds.Contains(stu.user_id)) continue;
                if (absentIds.Contains(stu.user_id)) {
                    result.Add(new { user_id = stu.user_id, student_id = stu.student_id, username = stu.username, first_name = stu.first_name, last_name = stu.last_name, mobile = stu.mobile, attendance_status = "absent" });
                } else {
                    result.Add(new { user_id = stu.user_id, student_id = stu.student_id, username = stu.username, first_name = stu.first_name, last_name = stu.last_name, mobile = stu.mobile, attendance_status = "pending" });
                }
            }

            return ServiceResult<object>.Ok(result);
        }

        public async Task<ServiceResult<object>> GetAttendanceStreakAsync(CancellationToken ct = default)
        {
            var thirtyDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
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
                    time_in = a.TimeIn,
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

            var studentExists = await _context.AccountsCustomusers.AnyAsync(u => u.Id == targetStudentId && u.Role.ToLower() == "student", ct);
            if (!studentExists)
                return ServiceResult<bool>.NotFound("Student not found");

            if (!DateOnly.TryParse(dto.Date, out var targetDate))
                targetDate = DateOnly.FromDateTime(DateTime.UtcNow);

            var isPresent = dto.IsPresent ?? true;

            var existingRecord = await _context.AttendanceAttendances
                .FirstOrDefaultAsync(a => a.StudentId == targetStudentId && a.Date == targetDate, ct);

            if (existingRecord != null)
            {
                existingRecord.IsPresent = isPresent;
                existingRecord.IsManual = true;
                existingRecord.Method = "MANUAL";
                if (!string.IsNullOrEmpty(dto.Note)) existingRecord.Note = dto.Note;
                existingRecord.MarkedAt = DateTime.UtcNow;
                if (isPresent && existingRecord.TimeIn == default)
                {
                     existingRecord.TimeIn = TimeOnly.FromDateTime(DateTime.UtcNow);
                }
            }
            else
            {
                var newRecord = new AttendanceAttendance
                {
                    StudentId = targetStudentId.Value,
                    Date = targetDate,
                    TimeIn = isPresent ? TimeOnly.FromDateTime(DateTime.UtcNow) : new TimeOnly(0, 0),
                    IsManual = true,
                    IsPresent = isPresent,
                    Method = "MANUAL",
                    Note = dto.Note,
                    MarkedAt = DateTime.UtcNow,
                };
                _context.AttendanceAttendances.Add(newRecord);
            }

            await _context.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<object>> RecordManualBulkAttendanceAsync(List<ManualAttendanceDto> dtos, CancellationToken ct = default)
        {
            if (dtos == null || dtos.Count == 0) return ServiceResult<object>.Fail("No attendance data provided");

            var studentIds = dtos.Where(d => d.StudentId.HasValue).Select(d => d.StudentId.Value).ToList();
            var mobiles = dtos.Where(d => d.StudentId == null && !string.IsNullOrEmpty(d.StudentMobile)).Select(d => d.StudentMobile).ToList();

            var studentsById = await _context.AccountsCustomusers.Where(u => studentIds.Contains(u.Id) && u.Role.ToLower() == "student").ToListAsync(ct);
            var studentsByMobile = await _context.AccountsCustomusers.Where(u => mobiles.Contains(u.Mobile) && u.Role.ToLower() == "student").ToListAsync(ct);

            var allValidStudentIds = studentsById.Select(s => s.Id).Concat(studentsByMobile.Select(s => s.Id)).ToHashSet();
            
            var parsedDates = dtos.Select(d => DateOnly.TryParse(d.Date, out var date) ? date : DateOnly.FromDateTime(DateTime.UtcNow)).Distinct().ToList();

            var existingRecords = await _context.AttendanceAttendances
                .Where(a => allValidStudentIds.Contains(a.StudentId) && parsedDates.Contains(a.Date))
                .ToListAsync(ct);

            foreach (var dto in dtos)
            {
                long? targetStudentId = dto.StudentId;
                if (targetStudentId == null && !string.IsNullOrEmpty(dto.StudentMobile))
                {
                    targetStudentId = studentsByMobile.FirstOrDefault(u => u.Mobile == dto.StudentMobile)?.Id;
                }

                if (targetStudentId == null || !allValidStudentIds.Contains(targetStudentId.Value)) continue;

                if (!DateOnly.TryParse(dto.Date, out var targetDate)) targetDate = DateOnly.FromDateTime(DateTime.UtcNow);
                var isPresent = dto.IsPresent ?? true;

                var existingRecord = existingRecords.FirstOrDefault(a => a.StudentId == targetStudentId && a.Date == targetDate);

                if (existingRecord != null)
                {
                    existingRecord.IsPresent = isPresent;
                    existingRecord.IsManual = true;
                    existingRecord.Method = "MANUAL";
                    if (!string.IsNullOrEmpty(dto.Note)) existingRecord.Note = dto.Note;
                    existingRecord.MarkedAt = DateTime.UtcNow;
                    if (isPresent && existingRecord.TimeIn == default) existingRecord.TimeIn = TimeOnly.FromDateTime(DateTime.UtcNow);
                }
                else
                {
                    var newRecord = new AttendanceAttendance
                    {
                        StudentId = targetStudentId.Value,
                        Date = targetDate,
                        TimeIn = isPresent ? TimeOnly.FromDateTime(DateTime.UtcNow) : new TimeOnly(0, 0),
                        IsManual = true,
                        IsPresent = isPresent,
                        Method = "MANUAL",
                        Note = dto.Note,
                        MarkedAt = DateTime.UtcNow,
                    };
                    _context.AttendanceAttendances.Add(newRecord);
                    existingRecords.Add(newRecord);
                }
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
                    time_in = a.TimeIn,
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
    }
}
