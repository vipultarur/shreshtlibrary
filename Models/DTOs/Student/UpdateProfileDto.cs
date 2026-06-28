using System.Text.Json.Serialization;

namespace WebApplication1.Models.DTOs.Student
{
    public class UpdateProfileDto
    {
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }
        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        [JsonPropertyName("goal")]
        public string? Goal { get; set; }
        [JsonPropertyName("dob")]
        public string? Dob { get; set; }
        [JsonPropertyName("caste")]
        public string? Caste { get; set; }
        [JsonPropertyName("address")]
        public string? Address { get; set; }
        [JsonPropertyName("parent_mobile")]
        public string? ParentMobile { get; set; }
    }
}
