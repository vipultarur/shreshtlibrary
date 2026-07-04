using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebApplication1.Models.DTOs.Attendance
{
    /// <summary>
    /// Payload for generating or regenerating a QR code.
    /// Moved from nested class in AdminAttendanceController (F009).
    /// </summary>
    public class QrGenerateDto
    {
        [JsonPropertyName("expiry_duration")]
        [MaxLength(20)]
        public string? ExpiryDuration { get; set; } // "1day", "7day", "1month"
    }
}
