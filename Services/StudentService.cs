using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Responses;
using WebApplication1.Models.DTOs.Student;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;

namespace WebApplication1.Services
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;

        public StudentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<object>?> GetProfileAsync(long userId, string scheme, string host, CancellationToken ct = default)
        {
            var profile = await _context.StudentsStudentprofiles
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);

            if (profile == null) return null;

            return ApiResponse<object>.Ok(new
            {
                username = profile.User.Username,
                first_name = profile.User.FirstName,
                last_name = profile.User.LastName,
                email = profile.User.Email,
                mobile = profile.User.Mobile,
                goal = profile.Goal,
                dob = profile.Dob?.ToString("yyyy-MM-dd"),
                caste = profile.Caste,
                address = profile.Address,
                profile_photo = !string.IsNullOrEmpty(profile.ProfilePhoto) ? $"{scheme}://{host}/media/{profile.ProfilePhoto}" : (string?)null,
                parent_mobile = profile.ParentMobile
            });
        }

        public async Task<ApiResponse<object>?> UpdateProfileAsync(long userId, UpdateProfileDto dto, CancellationToken ct = default)
        {
            var user = await _context.AccountsCustomusers
                .Include(u => u.StudentsStudentprofile)
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null || user.StudentsStudentprofile == null)
                return null;

            if (!string.IsNullOrEmpty(dto.FirstName))
                user.FirstName = dto.FirstName;
            if (!string.IsNullOrEmpty(dto.LastName))
                user.LastName = dto.LastName;
            if (!string.IsNullOrEmpty(dto.Email))
                user.Email = dto.Email;
            if (!string.IsNullOrEmpty(dto.Goal))
                user.StudentsStudentprofile.Goal = dto.Goal;
            if (!string.IsNullOrEmpty(dto.Dob) && System.DateOnly.TryParse(dto.Dob, out var dob))
                user.StudentsStudentprofile.Dob = dob;
            if (dto.Caste != null)
                user.StudentsStudentprofile.Caste = dto.Caste;
            if (dto.Address != null)
                user.StudentsStudentprofile.Address = dto.Address;
            if (dto.ParentMobile != null)
                user.StudentsStudentprofile.ParentMobile = dto.ParentMobile;

            user.StudentsStudentprofile.UpdatedAt = System.DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return ApiResponse<object>.Ok(new
            {
                username = user.Username,
                first_name = user.FirstName,
                last_name = user.LastName,
                email = user.Email,
                mobile = user.Mobile,
                goal = user.StudentsStudentprofile.Goal,
                dob = user.StudentsStudentprofile.Dob?.ToString("yyyy-MM-dd"),
                caste = user.StudentsStudentprofile.Caste,
                address = user.StudentsStudentprofile.Address,
                parent_mobile = user.StudentsStudentprofile.ParentMobile
            });
        }

        public async Task<ApiResponse<object>?> GetDashboardAsync(long userId, CancellationToken ct = default)
        {
            var profile = await _context.StudentsStudentprofiles
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);

            if (profile == null) return null;

            var fullName = $"{profile.User.FirstName} {profile.User.LastName}".Trim();
            var status = profile.Status ?? "PENDING";

            var todayDate = System.DateOnly.FromDateTime(System.DateTime.UtcNow);
            var activeMembership = await _context.MembershipsMemberships
                .Include(m => m.Plan)
                .Where(m => m.StudentId == userId && m.Status.ToLower() == "active")
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefaultAsync(ct);

            if (activeMembership != null && activeMembership.EndDate < todayDate)
            {
                activeMembership.Status = "expired";
                activeMembership.IsActive = false;
                
                var prof = await _context.StudentsStudentprofiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
                if (prof != null)
                {
                    prof.Status = "EXPIRED";
                    status = "EXPIRED";
                }
                
                await _context.SaveChangesAsync(ct);
                activeMembership = null;
            }

            string membershipPlan = "No Plan";
            int membershipDaysLeft = 0;
            bool isPremium = false;
            string membershipStatus = status;

            if (activeMembership != null)
            {
                membershipPlan = activeMembership.Plan?.Name ?? "Active Plan";
                var endDate = activeMembership.EndDate;
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
                expiryDialogMessage = !string.IsNullOrEmpty(profile.SuspensionReason) 
                    ? $"Your account has been suspended. Reason: {profile.SuspensionReason}"
                    : "Your account has been suspended. Please contact the library admin.";
            }
            else if (status == "EXPIRED" || (!isPremium && status != "PENDING"))
            {
                var appConfig = await _context.LibraryAppconfigs.FirstOrDefaultAsync(ct);
                bool premiumGating = appConfig?.IsPremiumGatingEnabled ?? true;
                
                if (premiumGating)
                {
                    // By default, restrict all major features
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

            var today = System.DateOnly.FromDateTime(System.DateTime.UtcNow);
            var markedAttendanceToday = await _context.AttendanceAttendances
                .AnyAsync(a => a.StudentId == userId && a.Date == today && a.IsPresent, ct);

            string assignedSeat = "—";
            string assignedSeatFloor = "—";
            var seatAssignment = await _context.SeatsSeatassignments
                .AsNoTracking()
                .Include(sa => sa.Seat)
                .Where(sa => sa.StudentId == userId && sa.ReleasedDate == null)
                .OrderByDescending(sa => sa.AssignedDate)
                .FirstOrDefaultAsync(ct);
                
            if (seatAssignment?.Seat != null)
            {
                assignedSeat = seatAssignment.Seat.SeatNumber;
                assignedSeatFloor = seatAssignment.Seat.Floor;
            }

            bool isHoliday = false;
            string? holidayTitle = null;
            string? holidayDescription = null;

            var razorpayKeySetting = await _context.CoreGlobalsettings
                .FirstOrDefaultAsync(s => s.Key == "RAZORPAY_KEY", ct);
            string razorpayKey = razorpayKeySetting?.Value ?? "";

            return ApiResponse<object>.Ok(new
            {
                student_id = profile.UserId,
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
                razorpay_key = razorpayKey
            });
        }

        public async Task<ApiResponse<object>?> GetIdCardAsync(long userId, string scheme, string host, CancellationToken ct = default)
        {
            var profile = await _context.StudentsStudentprofiles
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);

            if (profile == null) return null;

            return ApiResponse<object>.Ok(new
            {
                student_id = profile.UserId,
                full_name = $"{profile.User.FirstName} {profile.User.LastName}".Trim(),
                mobile = profile.User.Mobile,
                email = profile.User.Email,
                goal = profile.Goal,
                dob = profile.Dob?.ToString("yyyy-MM-dd"),
                photo_url = !string.IsNullOrEmpty(profile.ProfilePhoto) ? $"{scheme}://{host}/media/{profile.ProfilePhoto}" : (string?)null,
                qr_data = $"SHR-{profile.UserId}"
            });
        }

        public async Task<ApiResponse<object>> UploadPhotoAsync(long userId, IFormFile profile_photo, string scheme, string host, CancellationToken ct = default)
        {
            if (profile_photo == null || profile_photo.Length == 0)
                return ApiResponse<object>.Fail("No file uploaded");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(profile_photo.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext)) 
                return ApiResponse<object>.Fail("Invalid file type.");

            if (profile_photo.Length > 5 * 1024 * 1024) 
                return ApiResponse<object>.Fail("File size exceeds 5MB limit.");

            var profile = await _context.StudentsStudentprofiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (profile == null) return ApiResponse<object>.Fail("Profile not found");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "media", "profiles");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + ext;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                await profile_photo.CopyToAsync(fileStream, ct);
            }

            profile.ProfilePhoto = "profiles/" + uniqueFileName;
            await _context.SaveChangesAsync(ct);

            var photoUrl = $"{scheme}://{host}/media/{profile.ProfilePhoto}";
            return ApiResponse<object>.Ok(new { photo_url = photoUrl });
        }

        public async Task<ApiResponse<object>> GetReferralCodeAsync(long userId, CancellationToken ct = default)
        {
            var refCode = await _context.StudentsReferralcodes
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.StudentId == userId, ct);
            
            if (refCode == null)
            {
                return ApiResponse<object>.Fail("No referral code found. Please generate one.");
            }

            return ApiResponse<object>.Ok(new
            {
                id = refCode.Id,
                code = refCode.Code,
                used_by_count = refCode.UsedByCount,
                benefit_given = refCode.BenefitGiven
            });
        }

        public async Task<ApiResponse<object>> GenerateReferralCodeAsync(long userId, CancellationToken ct = default)
        {
            var existing = await _context.StudentsReferralcodes.FirstOrDefaultAsync(r => r.StudentId == userId, ct);
            if (existing != null)
            {
                return ApiResponse<object>.Fail("Referral code already exists.");
            }

            var profile = await _context.StudentsStudentprofiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (profile == null) return ApiResponse<object>.Fail("Profile not found");

            string code = $"REF{profile.StudentId?.Replace("-", "")}{new Random().Next(10, 99)}";
            var refCode = new WebApplication1.Models.StudentsReferralcode
            {
                StudentId = userId,
                Code = code,
                UsedByCount = 0,
                BenefitGiven = "1 month free extension"
            };
            _context.StudentsReferralcodes.Add(refCode);
            await _context.SaveChangesAsync(ct);

            return ApiResponse<object>.Ok(new
            {
                id = refCode.Id,
                code = refCode.Code,
                used_by_count = refCode.UsedByCount,
                benefit_given = refCode.BenefitGiven
            });
        }

        public async Task<ApiResponse<object>> ApplyReferralAsync(long userId, string code, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(code))
                return ApiResponse<object>.Fail("Referral code is required");

            var refCode = await _context.StudentsReferralcodes.FirstOrDefaultAsync(r => r.Code == code, ct);
            if (refCode == null)
                return ApiResponse<object>.Fail("Invalid referral code");

            if (refCode.StudentId == userId)
                return ApiResponse<object>.Fail("You cannot use your own referral code");

            var existing = await _context.StudentsReferralhistories.FirstOrDefaultAsync(h => h.ReferredStudentId == userId, ct);
            if (existing != null)
                return ApiResponse<object>.Fail("You have already applied a referral code");

            var history = new WebApplication1.Models.StudentsReferralhistory
            {
                ReferrerId = refCode.StudentId,
                ReferredStudentId = userId,
                AppliedAt = DateTime.UtcNow
            };

            refCode.UsedByCount++;
            
            _context.StudentsReferralhistories.Add(history);
            await _context.SaveChangesAsync(ct);

            return ApiResponse<object>.Ok(new { message = "Referral code applied successfully" });
        }

        public async Task<ApiResponse<object>> GetReferralHistoryAsync(long userId, CancellationToken ct = default)
        {
            var history = await _context.StudentsReferralhistories
                .AsNoTracking()
                .Include(h => h.ReferredStudent)
                .Where(h => h.ReferrerId == userId)
                .OrderByDescending(h => h.AppliedAt)
                .Take(100)
                .Select(h => new
                {
                    id = h.Id,
                    applied_at = h.AppliedAt.ToString("O"),
                    referred_student = h.ReferredStudent.FirstName + " " + h.ReferredStudent.LastName
                })
                .ToListAsync(ct);

            return ApiResponse<object>.Ok(history);
        }
    }
}
