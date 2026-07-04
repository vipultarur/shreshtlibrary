namespace WebApplication1.Models.DTOs.Admin
{
    public class AdminPayload
    {
        public string? Username { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("first_name")]
        public string? FirstName { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("last_name")]
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }
        public System.Collections.Generic.Dictionary<string, bool>? Permissions { get; set; }
    }
}
