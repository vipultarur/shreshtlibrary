using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.DTOs;
using WebApplication1.Models.Responses;

namespace WebApplication1.Services
{
    public class AdminSettingsService : IAdminSettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public AdminSettingsService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<ServiceResult<object>> GetSettingsAsync(string role, CancellationToken ct = default)
        {
            var cacheKey = $"AdminSettings_{role}";
            if (_cache.TryGetValue(cacheKey, out object? cachedObj) && cachedObj is System.Collections.Generic.Dictionary<string, object> cachedSettings)
            {
                return ServiceResult<object>.Ok(cachedSettings);
            }

            var appConfig = await _context.LibraryAppconfigs.OrderBy(a => a.Id).FirstOrDefaultAsync(ct);
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
                    UpdatedAt = DateTime.UtcNow,
                    EnableWhatsappService = false
                };
                _context.LibraryAppconfigs.Add(appConfig);
                await _context.SaveChangesAsync(ct);
            }

            var paddingSetting = await _context.CoreGlobalsettings
                .FirstOrDefaultAsync(s => s.Key == "ATTENDANCE_PADDING_MINUTES", ct);

            string paddingTime = paddingSetting?.Value ?? "60";

            var libraryInfo = await _context.LibraryLibraryinfos.AsNoTracking().OrderBy(l => l.Id).Select(l => new { l.OpeningTime }).FirstOrDefaultAsync(ct);
            var openTime = libraryInfo?.OpeningTime ?? new TimeOnly(10, 0);

            JsonElement safePermissions;
            try
            {
                safePermissions = JsonSerializer.Deserialize<JsonElement>(string.IsNullOrEmpty(appConfig.ExpiredStudentPermissions) ? "{}" : appConfig.ExpiredStudentPermissions);
            }
            catch
            {
                safePermissions = JsonSerializer.Deserialize<JsonElement>("{}");
            }

            var result = new System.Collections.Generic.Dictionary<string, object>
            {
                { "library_open_time", openTime.ToString(@"HH\:mm") },
                { "attendance_padding_time", paddingTime },
                { "is_premium_gating_enabled", appConfig.IsPremiumGatingEnabled },
                { "expiry_dialog_title", appConfig.ExpiryDialogTitle },
                { "expiry_dialog_message", appConfig.ExpiryDialogMessage },
                { "allow_non_premium_notifications", appConfig.AllowNonPremiumNotifications },
                { "allow_non_premium_sliders", appConfig.AllowNonPremiumSliders },
                { "allow_non_premium_library_info", appConfig.AllowNonPremiumLibraryInfo },
                { "expired_student_permissions", safePermissions },
                { "enable_whatsapp_service", appConfig.EnableWhatsappService },
                { "maintenance_mode", await GetMaintenanceMode(ct) }
            };

            if (role == "super_admin")
            {
                var waBaseUrl = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "wa_base_url", ct);
                var waSessionId = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "wa_session_id", ct);
                var waApiKey = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "wa_api_key", ct);

                result.Add("wa_base_url", waBaseUrl?.Value ?? "");
                result.Add("wa_session_id", waSessionId?.Value ?? "");
                result.Add("wa_api_key", waApiKey?.Value ?? "");
            }

            if (role == "super_admin" || role == "sub_super_admin")
            {
                var brevoApiKey = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "brevo_api_key", ct);
                var brevoFromName = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "brevo_from_name", ct);
                var brevoFromEmail = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "brevo_from_email", ct);

                result.Add("brevo_api_key", brevoApiKey?.Value ?? "");
                result.Add("brevo_from_name", brevoFromName?.Value ?? "");
                result.Add("brevo_from_email", brevoFromEmail?.Value ?? "");

                var enableEmailSystemStr = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "enable_email_system", ct);
                result.Add("enable_email_system", enableEmailSystemStr?.Value == "true");

                var cloudinaryCloudName = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "cloudinary_cloud_name", ct);
                var cloudinaryApiKey = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "cloudinary_api_key", ct);
                var cloudinaryApiSecret = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "cloudinary_api_secret", ct);

                result.Add("cloudinary_cloud_name", cloudinaryCloudName?.Value ?? "");
                result.Add("cloudinary_api_key", cloudinaryApiKey?.Value ?? "");
                result.Add("cloudinary_api_secret", cloudinaryApiSecret?.Value ?? "");
            }

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
            return ServiceResult<object>.Ok(result);
        }

        public async Task<ServiceResult<object>> UpdateSettingsAsync(SettingsPayload payload, string role, CancellationToken ct = default)
        {
            var appConfig = await _context.LibraryAppconfigs.OrderBy(a => a.Id).FirstOrDefaultAsync(ct);
            if (appConfig == null)
            {
                appConfig = new LibraryAppconfig { UpdatedAt = DateTime.UtcNow, ExpiredStudentPermissions = "{}" };
                _context.LibraryAppconfigs.Add(appConfig);
            }

            if (payload.EnableWhatsappService.HasValue)
                appConfig.EnableWhatsappService = payload.EnableWhatsappService.Value;

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

            async Task UpdateGlobalSetting(string key, string value, string desc, CancellationToken cancellationToken)
            {
                if (value == null) return;
                var setting = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
                if (setting == null)
                {
                    _context.CoreGlobalsettings.Add(new CoreGlobalsetting { Key = key, Value = value, Description = desc });
                }
                else
                {
                    setting.Value = value;
                }
            }

            if (role == "super_admin")
            {
                if (payload.MaintenanceMode.HasValue)
                {
                    await UpdateGlobalSetting("MAINTENANCE_MODE", payload.MaintenanceMode.Value ? "true" : "false", "App Maintenance Mode", ct);
                }

                await UpdateGlobalSetting("wa_base_url", payload.WaBaseUrl, "WhatsApp API Base URL", ct);
                await UpdateGlobalSetting("wa_session_id", payload.WaSessionId, "WhatsApp API Session ID", ct);
                if (!string.IsNullOrEmpty(payload.WaApiKey) && payload.WaApiKey != "******")
                {
                    await UpdateGlobalSetting("wa_api_key", payload.WaApiKey, "WhatsApp API Key", ct);
                }
            }

            if (role == "super_admin" || role == "sub_super_admin")
            {
                if (!string.IsNullOrEmpty(payload.BrevoApiKey) && payload.BrevoApiKey != "******")
                {
                    await UpdateGlobalSetting("brevo_api_key", payload.BrevoApiKey, "Brevo HTTP API Key", ct);
                }
                await UpdateGlobalSetting("brevo_from_name", payload.BrevoFromName, "Brevo From Name", ct);
                await UpdateGlobalSetting("brevo_from_email", payload.BrevoFromEmail, "Brevo From Email Address", ct);
                
                if (payload.EnableEmailSystem.HasValue)
                {
                    await UpdateGlobalSetting("enable_email_system", payload.EnableEmailSystem.Value ? "true" : "false", "Enable Email System", ct);
                }

                await UpdateGlobalSetting("cloudinary_cloud_name", payload.CloudinaryCloudName, "Cloudinary Cloud Name", ct);
                if (!string.IsNullOrEmpty(payload.CloudinaryApiKey) && payload.CloudinaryApiKey != "******")
                {
                    await UpdateGlobalSetting("cloudinary_api_key", payload.CloudinaryApiKey, "Cloudinary API Key", ct);
                }
                if (!string.IsNullOrEmpty(payload.CloudinaryApiSecret) && payload.CloudinaryApiSecret != "******")
                {
                    await UpdateGlobalSetting("cloudinary_api_secret", payload.CloudinaryApiSecret, "Cloudinary API Secret", ct);
                }
            }



            await _context.SaveChangesAsync(ct);
            _cache.Remove("LibraryInfo");
            _cache.Remove("AdminSettings_super_admin");
            _cache.Remove("AdminSettings_sub_super_admin");
            _cache.Remove("AdminSettings_admin");
            _cache.Remove("AdminSettings_");

            return await GetSettingsAsync(role, ct);
        }

        private async Task<bool> GetMaintenanceMode(CancellationToken ct)
        {
            var maintenanceSetting = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "MAINTENANCE_MODE", ct);
            return maintenanceSetting?.Value == "true";
        }
    }
}

