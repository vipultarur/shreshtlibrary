using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebApplication1.Models.DTOs.Admin
{
    /// <summary>
    /// Payload for creating or updating a student via the Admin API.
    /// Moved from nested class in AdminStudentsController (F009).
    /// </summary>
    public class StudentPayload
    {
        [JsonPropertyName("first_name")]
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        public string? FirstName { get; set; }

        [JsonPropertyName("middle_name")]
        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [JsonPropertyName("last_name")]
        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        public string? LastName { get; set; }

        [JsonPropertyName("email")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(254, ErrorMessage = "Email cannot exceed 254 characters.")]
        public string? Email { get; set; }

        [JsonPropertyName("mobile")]
        [Phone(ErrorMessage = "Invalid mobile number.")]
        [MaxLength(15, ErrorMessage = "Mobile cannot exceed 15 characters.")]
        public string? Mobile { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("goal")]
        [MaxLength(200)]
        public string? Goal { get; set; }

        [JsonPropertyName("dob")]
        public string? Dob { get; set; }

        [JsonPropertyName("gender")]
        [MaxLength(20)]
        public string? Gender { get; set; }

        [JsonPropertyName("caste")]
        [MaxLength(100)]
        public string? Caste { get; set; }

        [JsonPropertyName("address")]
        [MaxLength(500)]
        public string? Address { get; set; }

        [JsonPropertyName("parent_mobile")]
        [Phone]
        [MaxLength(15)]
        public string? ParentMobile { get; set; }

        [JsonPropertyName("status")]
        [MaxLength(50)]
        public string? Status { get; set; }

        [JsonPropertyName("preferred_language")]
        [MaxLength(10)]
        public string? PreferredLanguage { get; set; }

        [JsonPropertyName("username")]
        [MaxLength(150)]
        public string? Username { get; set; }

        [JsonPropertyName("password")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string? Password { get; set; }
    }

    /// <summary>Request to suspend a student with an optional reason.</summary>
    public class SuspendStudentRequest
    {
        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
