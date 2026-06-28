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
    }
}
