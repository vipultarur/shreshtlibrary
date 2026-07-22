using System.Text.Json.Serialization;

namespace WebApplication1.Models.Responses
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "success";

        [JsonPropertyName("message")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T? Data { get; set; }

        [JsonPropertyName("errors")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Errors { get; set; }

        [JsonPropertyName("version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? Version { get; set; }

        [JsonPropertyName("updated_at")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public System.DateTime? UpdatedAt { get; set; }

        public static ApiResponse<T> Ok(T? data, string? message = null, long? version = null, System.DateTime? updatedAt = null)
        {
            return new ApiResponse<T> { Success = true, Status = "success", Data = data, Message = message, Version = version, UpdatedAt = updatedAt };
        }

        public static ApiResponse<T> Fail(string? message, object? errors = null)
        {
            return new ApiResponse<T> { Success = false, Status = "error", Message = message, Errors = errors };
        }
    }
}
