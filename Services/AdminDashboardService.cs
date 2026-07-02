using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DTOs.Admin;

namespace WebApplication1.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public AdminDashboardService(ApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<object?> GetAdminProfileAsync(long userId, CancellationToken ct)
        {
            var user = await _context.AccountsAdminusers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null) return null;

            return new
            {
                username = user.Username,
                email = user.Email,
                mobile = user.Mobile,
                first_name = user.FirstName,
                last_name = user.LastName,
                role = user.Role,
                is_active = user.IsActive,
                date_joined = user.DateJoined,
                last_login = user.LastLogin,
                profile_image = !string.IsNullOrEmpty(user.ProfileImage) ? $"/media/{user.ProfileImage}" : null,
                permissions = !string.IsNullOrEmpty(user.Permissions) && user.Permissions != "{}" 
                    ? System.Text.Json.JsonSerializer.Deserialize<object>(user.Permissions) 
                    : (user.Role == "super_admin" 
                        ? new { SuperAdmin = true, Users = true, Billing = true, Settings = true, Reports = true } 
                        : new { Users = true, Attendance = true }),
                activity_count = await _context.CoreActivitylogs.CountAsync(l => l.AdminId == userId, ct),
                verified_payments_count = await _context.PaymentsPayments.CountAsync(p => p.VerifiedById == userId, ct),
                marked_attendance_count = await _context.AttendanceAttendances.CountAsync(a => a.MarkedById == userId, ct),
            };
        }

        public async Task<object?> UpdateAdminProfileAsync(long userId, AdminProfileUpdateDto request, string scheme, string host, CancellationToken ct)
        {
            var user = await _context.AccountsAdminusers.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null) return null;

            if (request.first_name != null) user.FirstName = request.first_name;
            if (request.last_name != null) user.LastName = request.last_name;
            if (request.email != null) user.Email = request.email;
            if (request.mobile != null) user.Mobile = request.mobile;

            if (request.profile_image != null)
            {
                var mediaDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", "shreshtlibrary", "media", "admins"));
                if (!System.IO.Directory.Exists(mediaDir)) System.IO.Directory.CreateDirectory(mediaDir);

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var ext = System.IO.Path.GetExtension(request.profile_image.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext)) throw new InvalidOperationException("Invalid file type.");
                
                var fileName = $"admin_{userId}_{Guid.NewGuid()}{ext}";
                var filePath = System.IO.Path.Combine(mediaDir, fileName);

                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 4096, System.IO.FileOptions.Asynchronous))
                {
                    await request.profile_image.CopyToAsync(stream, ct);
                }

                try
                {
                    user.ProfileImage = $"admins/{fileName}";
                    await _context.SaveChangesAsync(ct);
                }
                catch
                {
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                    throw;
                }
            }
            else
            {
                await _context.SaveChangesAsync(ct);
            }

            return new
            {
                username = user.Username,
                email = user.Email,
                mobile = user.Mobile,
                first_name = user.FirstName,
                last_name = user.LastName,
                role = user.Role,
                is_active = user.IsActive,
                date_joined = user.DateJoined,
                last_login = user.LastLogin,
                profile_image = !string.IsNullOrEmpty(user.ProfileImage) ? $"/media/{user.ProfileImage}" : null,
                permissions = !string.IsNullOrEmpty(user.Permissions) && user.Permissions != "{}" 
                    ? System.Text.Json.JsonSerializer.Deserialize<object>(user.Permissions) 
                    : (user.Role == "super_admin" 
                        ? new { SuperAdmin = true, Users = true, Billing = true, Settings = true, Reports = true } 
                        : new { Users = true, Attendance = true }),
                activity_count = await _context.CoreActivitylogs.CountAsync(l => l.AdminId == userId, ct),
                verified_payments_count = await _context.PaymentsPayments.CountAsync(p => p.VerifiedById == userId, ct),
                marked_attendance_count = await _context.AttendanceAttendances.CountAsync(a => a.MarkedById == userId, ct),
            };
        }

        public async Task<object> GetStatsOverviewAsync(string section, CancellationToken ct)
        {
            var nowUtc = _dateTimeProvider.UtcNow;
            var todayUtc = nowUtc.Date;
            var todayDateOnly = DateOnly.FromDateTime(todayUtc);
            var firstDayOfMonth = new DateTime(todayUtc.Year, todayUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            
            var studentStatusGroups = await _context.StudentsStudentprofiles.GroupBy(s => s.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(ct);
            var studentsLive = studentStatusGroups.FirstOrDefault(g => g.Status == "LIVE")?.Count ?? 0;
            var studentsExpired = studentStatusGroups.FirstOrDefault(g => g.Status == "EXPIRED")?.Count ?? 0;
            var studentsSuspended = studentStatusGroups.FirstOrDefault(g => g.Status == "SUSPENDED")?.Count ?? 0;
            var studentsTotal = studentStatusGroups.Where(g => g.Status != "EXPIRED" && g.Status != "SUSPENDED").Sum(g => g.Count);
            var studentsNewThisMonth = await _context.StudentsStudentprofiles.CountAsync(s => s.CreatedAt != null && s.CreatedAt >= firstDayOfMonth, ct);

            var attendanceGroups = await _context.AttendanceAttendances.Where(a => a.Date == todayDateOnly).ToListAsync(ct);
            var todayPresent = attendanceGroups.Count(a => a.IsPresent);
            var todaySystemAbsent = attendanceGroups.Count(a => !a.IsPresent && a.Method != "PENDING");
            var todayPending = attendanceGroups.Count(a => !a.IsPresent && a.Method == "PENDING");
            var todayUnaccounted = studentsTotal - attendanceGroups.Count;
            if (todayUnaccounted < 0) todayUnaccounted = 0;

            var libraryInfo = await _context.LibraryLibraryinfos.AsNoTracking().FirstOrDefaultAsync(ct);
            var attPaddingSetting = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "ATTENDANCE_PADDING_MINUTES", ct);
            var libOpenTime = libraryInfo?.OpeningTime ?? new TimeOnly(10, 0);
            int attPaddingMins = 60;
            if (attPaddingSetting != null && int.TryParse(attPaddingSetting.Value, out int attParsed)) attPaddingMins = attParsed;
            var attCutoff = libOpenTime.AddMinutes(attPaddingMins);
            var attCurrentTime = TimeOnly.FromDateTime(_dateTimeProvider.IstNow);

            int todayAbsent, finalPending;
            if (attCurrentTime > attCutoff)
            {
                todayAbsent = todaySystemAbsent + todayPending + todayUnaccounted;
                finalPending = 0;
            }
            else
            {
                todayAbsent = todaySystemAbsent;
                finalPending = todayPending + todayUnaccounted;
            }
            
            var nowIst = _dateTimeProvider.IstNow;
            var istZone = _dateTimeProvider.IstTimeZone;
            
            DateOnly todayIstDate = DateOnly.FromDateTime(nowIst);
            DateOnly monthIstDate = new DateOnly(nowIst.Year, nowIst.Month, 1);
            
            var todayIstMidnight = new DateTime(nowIst.Year, nowIst.Month, nowIst.Day, 0, 0, 0, DateTimeKind.Unspecified);
            DateTime todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(todayIstMidnight, istZone);
            DateTime todayEndUtc = TimeZoneInfo.ConvertTimeToUtc(todayIstMidnight.AddDays(1), istZone);
            
            var monthIstStart = new DateTime(nowIst.Year, nowIst.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            DateTime monthStartUtc = TimeZoneInfo.ConvertTimeToUtc(monthIstStart, istZone);
            
            var dailyRevenue = await _context.PaymentsPayments.Where(p => p.PaymentDate == todayIstDate && p.Status.ToLower() == "verified").SumAsync(p => (decimal?)p.Amount, ct) ?? 0;
            var monthlyRevenue = await _context.PaymentsPayments.Where(p => p.PaymentDate >= monthIstDate && p.PaymentDate <= todayIstDate && p.Status.ToLower() == "verified").SumAsync(p => (decimal?)p.Amount, ct) ?? 0;
            var pendingPayments = await _context.PaymentsPayments.CountAsync(p => p.Status.ToLower() == "pending", ct);

            var totalSeats = await _context.SeatsSeats.CountAsync(ct);
            var occupiedSeats = await _context.SeatsSeats.CountAsync(s => s.Status != null && s.Status.ToUpper() == "OCCUPIED", ct);
            var availableSeats = totalSeats - occupiedSeats;
            
            var genderGroups = await _context.StudentsStudentprofiles.GroupBy(s => s.Gender).Select(g => new { Gender = g.Key, Count = g.Count() }).ToListAsync(ct);
            var girls = genderGroups.Where(g => g.Gender.ToLower().StartsWith("f") || g.Gender.ToLower() == "girl").Sum(g => g.Count);
            var boys = genderGroups.Where(g => g.Gender.ToLower().StartsWith("m") || g.Gender.ToLower() == "boy").Sum(g => g.Count);
            var other = genderGroups.Where(g => !g.Gender.ToLower().StartsWith("f") && !g.Gender.ToLower().StartsWith("m") && g.Gender.ToLower() != "girl" && g.Gender.ToLower() != "boy").Sum(g => g.Count);

            var paymentsMonthCount = await _context.PaymentsPayments.CountAsync(p => p.PaymentDate >= monthIstDate && p.PaymentDate <= todayIstDate && p.Status.ToLower() == "verified", ct);

            return new
            {
                students = new {
                    total = studentsTotal,
                    live = studentsLive,
                    expired = studentsExpired,
                    suspended = studentsSuspended,
                    pending = studentStatusGroups.FirstOrDefault(g => g.Status == "PENDING")?.Count ?? 0,
                    girls = girls,
                    boys = boys,
                    other = other
                },
                payments = new {
                    month_amount = monthlyRevenue,
                    month_count = paymentsMonthCount
                },
                attendance = new {
                    today_present = todayPresent,
                    today_absent = todayAbsent,
                    today_pending = finalPending
                },
                seats = new {
                    total = totalSeats,
                    available = availableSeats,
                    occupied = occupiedSeats
                }
            };
        }

        public async Task<object> GetDashboardChartsAsync(string range, CancellationToken ct)
        {
            int days = range == "month" ? 30 : (range == "week" ? 7 : 30);
            var nowUtc = _dateTimeProvider.UtcNow;
            
            var minDate = DateOnly.FromDateTime(nowUtc.AddDays(-days));
            var maxDate = DateOnly.FromDateTime(nowUtc);
            
            var attGroups = await _context.AttendanceAttendances
                .Where(a => a.Date >= minDate && a.Date <= maxDate && a.IsPresent)
                .GroupBy(a => a.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var attendanceDates = new System.Collections.Generic.List<string>();
            var attendanceCounts = new System.Collections.Generic.List<int>();
            for(int i=days; i>=0; i--)
            {
                var d = DateOnly.FromDateTime(nowUtc.AddDays(-i));
                attendanceDates.Add(d.ToString("MMM dd"));
                attendanceCounts.Add(attGroups.FirstOrDefault(g => g.Date == d)?.Count ?? 0);
            }

            var monthsBack = 6;
            var minMonthDate = nowUtc.AddMonths(-monthsBack);
            var minMonthDateOnly = DateOnly.FromDateTime(minMonthDate);
            var paymentGroups = await _context.PaymentsPayments
                .Where(p => p.PaymentDate >= minMonthDateOnly && p.Status.ToLower() == "verified")
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Revenue = g.Sum(p => (decimal?)p.Amount) ?? 0, Count = g.Count() })
                .ToListAsync(ct);

            var revenueLabels = new System.Collections.Generic.List<string>();
            var revenueData = new System.Collections.Generic.List<decimal>();
            for (int i=monthsBack-1; i>=0; i--)
            {
                var d = nowUtc.AddMonths(-i);
                revenueLabels.Add(d.ToString("MMM yyyy"));
                var g = paymentGroups.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month);
                revenueData.Add(g?.Revenue ?? 0);
            }

            var studySessions = await _context.StudyStudysessions
                .AsNoTracking()
                .Where(s => s.StartTime >= nowUtc.AddDays(-7) && s.Status == "completed")
                .ToListAsync(ct);
                
            var studyDates = new System.Collections.Generic.List<string>();
            var studyHours = new System.Collections.Generic.List<double>();
            
            for(int i=6; i>=0; i--)
            {
                var d = nowUtc.AddDays(-i).Date;
                studyDates.Add(d.ToString("MMM dd"));
                var nextD = d.AddDays(1);
                var mins = studySessions.Where(s => s.StartTime >= d && s.StartTime < nextD).Sum(s => s.DurationMinutes);
                studyHours.Add(Math.Round(mins / 60.0, 1));
            }

            var totalSeatsCount = await _context.SeatsSeats.CountAsync(ct);
            var occupied = await _context.SeatsSeats.CountAsync(s => s.Status != null && s.Status.ToUpper() == "OCCUPIED", ct);
            var available = totalSeatsCount - occupied;

            return new
            {
                attendance_trend = new { labels = attendanceDates, data = attendanceCounts },
                revenue_trend = new { labels = revenueLabels, data = revenueData },
                study_hours = new { labels = studyDates, data = studyHours },
                seat_occupancy = new { labels = new[] { "Available", "Occupied" }, data = new[] { available, occupied } }
            };
        }



        public async Task<object> GetDashboardAlertsAsync(CancellationToken ct)
        {
            var alerts = new System.Collections.Generic.List<object>();
            var pendingPayments = await _context.PaymentsPayments.CountAsync(p => p.Status.ToLower() == "pending", ct);
            if (pendingPayments > 0)
            {
                alerts.Add(new {
                    type = "pending_payments",
                    label = "Pending Payments",
                    count = pendingPayments
                });
            }
            
            var unreadMessages = await _context.NotificationsAdmininboxnotifications.CountAsync(n => !n.IsRead, ct);
            if (unreadMessages > 0)
            {
                alerts.Add(new {
                    type = "unread_messages",
                    label = "Unread Messages",
                    count = unreadMessages
                });
            }

            return alerts;
        }

        public async Task<object> GetRecentActivityAsync(CancellationToken ct)
        {
            var activity = await _context.CoreActivitylogs
                .AsNoTracking()
                .Include(a => a.User)
                .Include(a => a.Admin)
                .OrderByDescending(a => a.Timestamp)
                .Take(15)
                .Select(a => new {
                    id = a.Id,
                    action = a.Action,
                    description = a.Details,
                    admin_name = a.User != null ? a.User.Username : (a.Admin != null ? a.Admin.Username : "System"),
                    created_at = a.Timestamp
                })
                .ToListAsync(ct);
            return activity;
        }

        public async Task<object> GetAttendanceOverviewChartsAsync(CancellationToken ct)
        {
            var nowUtc = _dateTimeProvider.UtcNow;
            var days = 7;
            var minDate = DateOnly.FromDateTime(nowUtc.AddDays(-days));
            var maxDate = DateOnly.FromDateTime(nowUtc);
            
            var attGroups = await _context.AttendanceAttendances
                .Where(a => a.Date >= minDate && a.Date <= maxDate && a.IsPresent)
                .GroupBy(a => a.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var labels = new System.Collections.Generic.List<string>();
            var data = new System.Collections.Generic.List<int>();
            for(int i=days-1; i>=0; i--)
            {
                var d = DateOnly.FromDateTime(nowUtc.AddDays(-i));
                labels.Add(d.ToString("MMM dd"));
                data.Add(attGroups.FirstOrDefault(g => g.Date == d)?.Count ?? 0);
            }

            return new {
                labels = labels,
                present = data
            };
        }

        public async Task<object> GetRevenueOverviewChartsAsync(CancellationToken ct)
        {
            var nowUtc = _dateTimeProvider.UtcNow;
            var sixMonthsAgo = DateOnly.FromDateTime(nowUtc.AddMonths(-6));

            var paymentGroups = await _context.PaymentsPayments
                .Where(p => p.PaymentDate >= sixMonthsAgo && p.Status.ToLower() == "verified")
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Revenue = g.Sum(p => (decimal?)p.Amount) ?? 0 })
                .ToListAsync(ct);

            var labels = new System.Collections.Generic.List<string>();
            var data = new System.Collections.Generic.List<decimal>();
            for (int i = 5; i >= 0; i--)
            {
                var d = nowUtc.AddMonths(-i);
                labels.Add(d.ToString("MMM yyyy"));
                var g = paymentGroups.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month);
                data.Add(g?.Revenue ?? 0);
            }

            return new { labels = labels, revenue = data };
        }

        public async Task<object> GetStudentsOverviewChartsAsync(CancellationToken ct)
        {
            var studentGoalGroups = await _context.StudentsStudentprofiles
                .GroupBy(s => string.IsNullOrEmpty(s.Goal) ? "Unspecified" : s.Goal)
                .Select(g => new { goal = g.Key, students = g.Count() })
                .OrderByDescending(x => x.students)
                .ToListAsync(ct);

            return new { items = studentGoalGroups };
        }

        public async Task<object> GetMembershipsOverviewChartsAsync(CancellationToken ct)
        {
            var membershipGroups = await _context.MembershipsMemberships
                .Include(m => m.Plan)
                .Where(m => m.IsActive)
                .GroupBy(m => m.Plan.Name)
                .Select(g => new { name = g.Key, active = g.Count() })
                .OrderByDescending(x => x.active)
                .ToListAsync(ct);

            return new { items = membershipGroups };
        }
    }
}

