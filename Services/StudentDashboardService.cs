using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Responses;
using System.Threading;
using System;

namespace WebApplication1.Services
{
    public class StudentDashboardService : IStudentDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public StudentDashboardService(ApplicationDbContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<ApiResponse<object>?> GetDashboardAsync(long userId, CancellationToken ct = default)
        {
            var todayDate = System.DateOnly.FromDateTime(_dateTimeProvider.IstNow);
            var today = todayDate;

            // 1. Handle Expirations First
            var expiringMembership = await _context.MembershipsMemberships
                .Where(m => m.StudentId == userId && m.Status.ToLower() == "active" && m.EndDate < todayDate)
                .FirstOrDefaultAsync(ct);

            if (expiringMembership != null)
            {
                expiringMembership.Status = "expired";
                expiringMembership.IsActive = false;
                var prof = await _context.StudentsStudentprofiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
                if (prof != null)
                {
                    prof.Status = "EXPIRED";
                }
                await _context.SaveChangesAsync(ct);
            }

            // 2. Fetch everything else in a single projected query
            var data = await _context.StudentsStudentprofiles
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => new
                {
                    Profile = s,
                    User = s.User,
                    ActiveMembership = _context.MembershipsMemberships
                        .Where(m => m.StudentId == userId && m.Status.ToLower() == "active")
                        .OrderByDescending(m => m.EndDate)
                        .Select(m => new { Membership = m, PlanName = m.Plan.Name })
                        .FirstOrDefault(),
                    AttendanceToday = _context.AttendanceAttendances
                        .FirstOrDefault(a => a.StudentId == userId && a.Date == today),
                    SeatAssignment = _context.SeatsSeatassignments
                        .Where(sa => sa.StudentId == userId && sa.ReleasedDate == null)
                        .OrderByDescending(sa => sa.AssignedDate)
                        .Select(sa => new { Assignment = sa, SeatNumber = sa.Seat.SeatNumber, Floor = sa.Seat.Floor })
                        .FirstOrDefault(),
                    HolidayRecord = _context.AttendanceHolidays
                        .FirstOrDefault(h => h.Date == today && h.IsActive),
                    AppConfig = _context.LibraryAppconfigs.OrderBy(a => a.Id).FirstOrDefault(),
                    LibraryInfo = _context.LibraryLibraryinfos.OrderBy(l => l.Id).Select(l => new { l.OpeningTime, l.ClosingTime }).FirstOrDefault(),
                    PaddingSetting = _context.CoreGlobalsettings.Where(gs => gs.Key == "ATTENDANCE_PADDING_MINUTES").Select(gs => gs.Value).FirstOrDefault(),
                    RazorpayKey = _context.CoreGlobalsettings.Where(gs => gs.Key == "RAZORPAY_KEY").Select(gs => gs.Value).FirstOrDefault()
                })
                .FirstOrDefaultAsync(ct);

            if (data == null) return null;

            var fullName = $"{data.User.FirstName} {data.User.LastName}".Trim();
            var status = data.Profile.Status ?? "PENDING";

            string membershipPlan = "No Plan";
            int membershipDaysLeft = 0;
            bool isPremium = false;
            string membershipStatus = status;

            if (data.ActiveMembership != null)
            {
                membershipPlan = data.ActiveMembership.PlanName ?? "Active Plan";
                var endDate = data.ActiveMembership.Membership.EndDate;
                membershipDaysLeft = (endDate.ToDateTime(System.TimeOnly.MinValue) - System.DateTime.UtcNow).Days;
                if (membershipDaysLeft < 0) membershipDaysLeft = 0;
                isPremium = true;
                membershipStatus = "LIVE";
            }

            var restrictedFeatures = new System.Collections.Generic.List<string>();
            string? expiryDialogTitle = null;
            string? expiryDialogMessage = null;

            if (status == "PENDING")
            {
                restrictedFeatures.AddRange(new[] { "attendance", "study", "seats", "payments", "sliders" });
                membershipStatus = "PENDING";
                membershipPlan = "No Plan";
                membershipDaysLeft = 0;
                isPremium = false;
                expiryDialogTitle = "Pending Activation";
                expiryDialogMessage = "Your account is pending. Please purchase a membership plan or contact the library admin to activate your account.";
            }
            else if (status == "SUSPENDED")
            {
                restrictedFeatures.AddRange(new[] { "attendance", "study", "seats", "payments", "sliders", "notifications" });
                membershipStatus = "SUSPENDED";
                expiryDialogTitle = "Account Suspended";
                expiryDialogMessage = !string.IsNullOrEmpty(data.Profile.SuspensionReason) 
                    ? $"Your account has been suspended. Reason: {data.Profile.SuspensionReason}"
                    : "Your account has been suspended. Please contact the library admin.";
            }
            else if (status == "EXPIRED" || (!isPremium && status != "PENDING"))
            {
                var appConfig = data.AppConfig;
                bool premiumGating = appConfig?.IsPremiumGatingEnabled ?? true;
                
                if (premiumGating)
                {
                    var allFeatures = new System.Collections.Generic.HashSet<string> { "attendance", "study", "seats", "payments", "sliders", "notifications" };
                    var allowedFeatures = new System.Collections.Generic.HashSet<string>();

                    if (appConfig != null)
                    {
                        if (appConfig.AllowNonPremiumNotifications) allowedFeatures.Add("notifications");
                        if (appConfig.AllowNonPremiumSliders) allowedFeatures.Add("sliders");

                        if (!string.IsNullOrEmpty(appConfig.ExpiredStudentPermissions))
                        {
                            try
                            {
                                var parsedPerms = System.Text.Json.JsonDocument.Parse(appConfig.ExpiredStudentPermissions);
                                if (parsedPerms.RootElement.TryGetProperty("allowed_paths", out var allowedPaths))
                                {
                                    var paths = allowedPaths.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                                    if (paths.Contains("/api/v1/payments/")) allowedFeatures.Add("payments");
                                    if (paths.Contains("/api/v1/study/leaderboard/")) allowedFeatures.Add("study");
                                    if (paths.Contains("/api/v1/notifications/")) allowedFeatures.Add("notifications");
                                }
                            }
                            catch { }
                        }
                    }

                    foreach (var feat in allFeatures)
                    {
                        if (!allowedFeatures.Contains(feat))
                        {
                            restrictedFeatures.Add(feat);
                        }
                    }
                }

                membershipStatus = "EXPIRED";
                expiryDialogTitle = appConfig?.ExpiryDialogTitle ?? "Membership Expired";
                expiryDialogMessage = appConfig?.ExpiryDialogMessage ?? "Your membership has expired. Please renew your plan to continue accessing all features.";
            }

            bool markedAttendanceToday = data.AttendanceToday != null && data.AttendanceToday.IsPresent;
            string assignedSeat = data.SeatAssignment?.SeatNumber ?? "—";
            string assignedSeatFloor = data.SeatAssignment?.Floor ?? "—";
            bool isHoliday = data.HolidayRecord != null;
            string? holidayTitle = data.HolidayRecord?.Title;
            string? holidayDescription = data.HolidayRecord?.Description;

            string attendanceStatus = "Pending";
            string? attendanceTime = null;
            var openTime = data.LibraryInfo?.OpeningTime ?? new System.TimeOnly(10, 0);
            var closeTime = data.LibraryInfo?.ClosingTime ?? new System.TimeOnly(22, 0);
            int paddingMinutes = 60;
            if (data.PaddingSetting != null && int.TryParse(data.PaddingSetting, out int parsedPadding))
            {
                paddingMinutes = parsedPadding;
            }
            
            DateTime todayCutoffDateTime;
            if (closeTime < openTime)
            {
                todayCutoffDateTime = todayDate.ToDateTime(closeTime).AddDays(1).AddMinutes(paddingMinutes);
            }
            else
            {
                todayCutoffDateTime = todayDate.ToDateTime(closeTime).AddMinutes(paddingMinutes);
            }
            
            DateTime todayOpenDateTime = todayDate.ToDateTime(openTime);
            
            bool isPastCutoff = _dateTimeProvider.IstNow > todayCutoffDateTime;
            bool isOpen = _dateTimeProvider.IstNow >= todayOpenDateTime && _dateTimeProvider.IstNow <= todayCutoffDateTime;

            bool allowQrScan = false;

            if (isHoliday)
            {
                attendanceStatus = "Holiday";
            }
            else if (data.AttendanceToday != null)
            {
                if (data.AttendanceToday.IsPresent)
                {
                    if (data.AttendanceToday.MarkedAt.HasValue)
                    {
                        var istMarkedAt = TimeZoneInfo.ConvertTimeFromUtc(data.AttendanceToday.MarkedAt.Value, _dateTimeProvider.IstTimeZone);
                        attendanceTime = istMarkedAt.ToString("hh:mm tt");
                    }
                    else
                    {
                        attendanceTime = data.AttendanceToday.TimeIn.ToString("hh:mm tt");
                    }
                    attendanceStatus = data.AttendanceToday.LateMark ? "Present (Arrived Late)" : "Present";
                }
                else if (data.AttendanceToday.Method == "PENDING")
                {
                    if (isPastCutoff)
                    {
                        attendanceStatus = "Absent";
                    }
                    else
                    {
                        attendanceStatus = "Pending";
                        allowQrScan = isOpen;
                    }
                }
                else
                {
                    attendanceStatus = "Absent";
                    if (data.AttendanceToday.MarkedAt.HasValue)
                    {
                        var istMarkedAt = TimeZoneInfo.ConvertTimeFromUtc(data.AttendanceToday.MarkedAt.Value, _dateTimeProvider.IstTimeZone);
                        attendanceTime = istMarkedAt.ToString("hh:mm tt");
                    }
                }
            }
            else
            {
                if (isPastCutoff)
                {
                    attendanceStatus = "Absent";
                }
                else
                {
                    attendanceStatus = "Pending";
                    allowQrScan = isOpen;
                }
            }

            if (restrictedFeatures.Contains("attendance"))
            {
                allowQrScan = false;
            }

            string razorpayKey = data.RazorpayKey ?? "";

            return ApiResponse<object>.Ok(new
            {
                student_id = data.Profile.UserId,
                full_name = fullName,
                membership_plan = membershipPlan,
                membership_days_left = membershipDaysLeft,
                is_premium = isPremium,
                membership_status = membershipStatus,
                restricted_features = restrictedFeatures,
                expiry_dialog = (expiryDialogTitle != null) ? new { title = expiryDialogTitle, message = expiryDialogMessage } : null,
                assigned_seat = assignedSeat,
                assigned_seat_floor = assignedSeatFloor,
                marked_attendance_today = markedAttendanceToday,
                is_holiday = isHoliday,
                holiday_title = holidayTitle,
                holiday_description = holidayDescription,
                attendance_status = attendanceStatus,
                attendance_time = attendanceTime,
                allow_qr_scan = allowQrScan,
                razorpay_key = razorpayKey
            });
        }
    }
}
