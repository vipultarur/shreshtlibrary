using System.Text.Json.Serialization;

namespace WebApplication1.Models.DTOs.Attendance
{
    public class ManualAttendanceDto
    {
        [JsonPropertyName("student_id")]
        public long? StudentId { get; set; }

        [JsonPropertyName("student_mobile")]
        public string? StudentMobile { get; set; }

        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("is_present")]
        public bool? IsPresent { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }
    }
}
