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

        public async Task<ServiceResult<object>> GetSettingsAsync(string role, CancellationToken ct = default)
        {
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
                { "enable_whatsapp_service", appConfig.EnableWhatsappService }
            };

            if (role == "super_admin")
            {
                var smtpHost = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "smtp_host", ct);
                var smtpPort = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "smtp_port", ct);
                var smtpUser = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "smtp_user", ct);
                var smtpPass = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "smtp_pass", ct);
                var smtpFromName = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "smtp_from_name", ct);
                var smtpFromEmail = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "smtp_from_email", ct);
                var waBaseUrl = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "wa_base_url", ct);
                var waSessionId = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "wa_session_id", ct);
                var waApiKey = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == "wa_api_key", ct);

                result.Add("smtp_host", smtpHost?.Value ?? "");
                result.Add("smtp_port", smtpPort?.Value ?? "");
                result.Add("smtp_user", smtpUser?.Value ?? "");
                result.Add("smtp_pass", smtpPass?.Value ?? "");
                result.Add("smtp_from_name", smtpFromName?.Value ?? "");
                result.Add("smtp_from_email", smtpFromEmail?.Value ?? "");
                
                result.Add("wa_base_url", waBaseUrl?.Value ?? "");
                result.Add("wa_session_id", waSessionId?.Value ?? "");
                result.Add("wa_api_key", waApiKey?.Value ?? "");
            }

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

            if (role == "super_admin")
            {
                async Task UpdateGlobalSetting(string key, string value, string desc)
                {
                    if (value == null) return;
                    var setting = await _context.CoreGlobalsettings.FirstOrDefaultAsync(s => s.Key == key, ct);
                    if (setting == null)
                    {
                        _context.CoreGlobalsettings.Add(new CoreGlobalsetting { Key = key, Value = value, Description = desc });
                    }
                    else
                    {
                        setting.Value = value;
                    }
                }

                await UpdateGlobalSetting("smtp_host", payload.SmtpHost, "SMTP Host Server");
                await UpdateGlobalSetting("smtp_port", payload.SmtpPort, "SMTP Port");
                await UpdateGlobalSetting("smtp_user", payload.SmtpUser, "SMTP Username");
                if (!string.IsNullOrEmpty(payload.SmtpPass) && payload.SmtpPass != "******")
                {
                    await UpdateGlobalSetting("smtp_pass", payload.SmtpPass, "SMTP App Password");
                }
                await UpdateGlobalSetting("smtp_from_name", payload.SmtpFromName, "SMTP From Name");
                await UpdateGlobalSetting("smtp_from_email", payload.SmtpFromEmail, "SMTP From Email Address");
                
                await UpdateGlobalSetting("wa_base_url", payload.WaBaseUrl, "WhatsApp API Base URL");
                await UpdateGlobalSetting("wa_session_id", payload.WaSessionId, "WhatsApp API Session ID");
                if (!string.IsNullOrEmpty(payload.WaApiKey) && payload.WaApiKey != "******")
                {
                    await UpdateGlobalSetting("wa_api_key", payload.WaApiKey, "WhatsApp API Key");
                }
            }

            await _context.SaveChangesAsync(ct);

            return await GetSettingsAsync(role, ct);
        }
    }
}

