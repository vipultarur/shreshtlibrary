using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.DTOs;
using WebApplication1.Models.Responses;

namespace WebApplication1.Services
{
    public class AdminSettingsService : IAdminSettingsService
    {
        private readonly ApplicationDbContext _context;

        public AdminSettingsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<object>> GetSettingsAsync(CancellationToken ct = default)
        {
            var appConfig = await _context.LibraryAppconfigs.FirstOrDefaultAsync(ct);
            if (appConfig == null)
            {
                appConfig = new LibraryAppconfig
                {
                    IsPremiumGatingEnabled = true,
                    ExpiryDialogTitle = "Plan Expired",
                    ExpiryDialogMessage = "Your plan has expired. Please renew to continue using premium features.",
                    AllowNonPremiumNotifications = true,
                    AllowNonPremiumLibraryInfo = true,
                    AllowNonPremiumSliders = true,
                    DefaultAllowedStudyMinutes = 0,
                    ExpiredStudentPermissions = "{\"allowed_paths\":[\"/api/v1/auth/\",\"/api/v1/student/profile/\",\"/api/v1/memberships/plans/\",\"/api/v1/payments/\",\"/api/v1/study/leaderboard/\",\"/api/v1/notifications/\"]}",
                    UpdatedAt = DateTime.UtcNow
                };
                _context.LibraryAppconfigs.Add(appConfig);
                await _context.SaveChangesAsync(ct);
            }

            var paddingSetting = await _context.CoreGlobalsettings
                .FirstOrDefaultAsync(s => s.Key == "ATTENDANCE_PADDING_MINUTES", ct);

            string paddingTime = paddingSetting?.Value ?? "60";

            return ServiceResult<object>.Ok(new
            {
                attendance_padding_time = paddingTime,
                is_premium_gating_enabled = appConfig.IsPremiumGatingEnabled,
                expiry_dialog_title = appConfig.ExpiryDialogTitle,
                expiry_dialog_message = appConfig.ExpiryDialogMessage,
                allow_non_premium_notifications = appConfig.AllowNonPremiumNotifications,
                allow_non_premium_sliders = appConfig.AllowNonPremiumSliders,
                allow_non_premium_library_info = appConfig.AllowNonPremiumLibraryInfo,
                expired_student_permissions = JsonSerializer.Deserialize<JsonElement>(appConfig.ExpiredStudentPermissions)
            });
        }

        public async Task<ServiceResult<object>> UpdateSettingsAsync(SettingsPayload payload, CancellationToken ct = default)
        {
            var appConfig = await _context.LibraryAppconfigs.FirstOrDefaultAsync(ct);
            if (appConfig == null)
            {
                appConfig = new LibraryAppconfig { UpdatedAt = DateTime.UtcNow, ExpiredStudentPermissions = "{}" };
                _context.LibraryAppconfigs.Add(appConfig);
            }

            if (payload.IsPremiumGatingEnabled.HasValue)
                appConfig.IsPremiumGatingEnabled = payload.IsPremiumGatingEnabled.Value;
            
            if (payload.ExpiryDialogTitle != null)
                appConfig.ExpiryDialogTitle = payload.ExpiryDialogTitle;
            
            if (payload.ExpiryDialogMessage != null)
                appConfig.ExpiryDialogMessage = payload.ExpiryDialogMessage;

            if (payload.AllowNonPremiumNotifications.HasValue)
                appConfig.AllowNonPremiumNotifications = payload.AllowNonPremiumNotifications.Value;

            if (payload.AllowNonPremiumSliders.HasValue)
                appConfig.AllowNonPremiumSliders = payload.AllowNonPremiumSliders.Value;

            if (payload.AllowNonPremiumLibraryInfo.HasValue)
                appConfig.AllowNonPremiumLibraryInfo = payload.AllowNonPremiumLibraryInfo.Value;

            if (payload.ExpiredStudentPermissions.HasValue)
                appConfig.ExpiredStudentPermissions = JsonSerializer.Serialize(payload.ExpiredStudentPermissions.Value);

            appConfig.UpdatedAt = DateTime.UtcNow;

            if (payload.AttendancePaddingTime != null)
            {
                var paddingSetting = await _context.CoreGlobalsettings
                    .FirstOrDefaultAsync(s => s.Key == "ATTENDANCE_PADDING_MINUTES", ct);
                
                if (paddingSetting == null)
                {
                    paddingSetting = new CoreGlobalsetting
                    {
                        Key = "ATTENDANCE_PADDING_MINUTES",
                        Value = payload.AttendancePaddingTime,
                        Description = "Attendance Padding Time (Minutes)"
                    };
                    _context.CoreGlobalsettings.Add(paddingSetting);
                }
                else
                {
                    paddingSetting.Value = payload.AttendancePaddingTime;
                }
            }

            await _context.SaveChangesAsync(ct);

            return await GetSettingsAsync(ct);
        }
    }
}
