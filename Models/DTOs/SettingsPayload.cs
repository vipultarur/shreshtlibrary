using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApplication1.Models.DTOs
{
    public class SettingsPayload
    {
        [JsonPropertyName("attendance_padding_time")]
        public string? AttendancePaddingTime { get; set; }

        [JsonPropertyName("is_premium_gating_enabled")]
        public bool? IsPremiumGatingEnabled { get; set; }

        [JsonPropertyName("expiry_dialog_title")]
        public string? ExpiryDialogTitle { get; set; }

        [JsonPropertyName("expiry_dialog_message")]
        public string? ExpiryDialogMessage { get; set; }

        [JsonPropertyName("allow_non_premium_notifications")]
        public bool? AllowNonPremiumNotifications { get; set; }

        [JsonPropertyName("allow_non_premium_sliders")]
        public bool? AllowNonPremiumSliders { get; set; }

        [JsonPropertyName("allow_non_premium_library_info")]
        public bool? AllowNonPremiumLibraryInfo { get; set; }

        [JsonPropertyName("expired_student_permissions")]
        public JsonElement? ExpiredStudentPermissions { get; set; }

        [JsonPropertyName("brevo_api_key")]
        public string? BrevoApiKey { get; set; }

        [JsonPropertyName("brevo_from_name")]
        public string? BrevoFromName { get; set; }

        [JsonPropertyName("brevo_from_email")]
        public string? BrevoFromEmail { get; set; }

        [JsonPropertyName("wa_base_url")]
        public string? WaBaseUrl { get; set; }

        [JsonPropertyName("wa_session_id")]
        public string? WaSessionId { get; set; }

        [JsonPropertyName("wa_api_key")]
        public string? WaApiKey { get; set; }

        [JsonPropertyName("enable_whatsapp_service")]
        public bool? EnableWhatsappService { get; set; }

        [JsonPropertyName("maintenance_mode")]
        public bool? MaintenanceMode { get; set; }
    }
}
