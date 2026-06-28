using System.Collections.Generic;

namespace WebApplication1.Services
{
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
        public T? Data { get; set; }
        public bool IsNotFound { get; set; }
        
        public static ServiceResult<T> Ok(T? data, string? message = null) => new() { Success = true, Data = data, Message = message };
        public static ServiceResult<T> Fail(string? message, Dictionary<string, string[]>? errors = null) => new() { Success = false, Message = message, Errors = errors };
        public static ServiceResult<T> NotFound(string? message = "Resource not found") => new() { Success = false, IsNotFound = true, Message = message };
    }
}
