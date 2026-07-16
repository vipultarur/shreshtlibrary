using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;
using WebApplication1.Models;
using WebApplication1.Models.DTOs.Auth;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Threading;
using System.Linq;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("AnonRateThrottle")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        private IActionResult HandleResult(ServiceResult<object> result)
        {
            if (!result.Success)
            {
                if (result.IsNotFound) return NotFound(new { success = false, status = "error", message = result.Message, errors = result.Errors });
                
                if (result.Message != null && (result.Message.Contains("revoke") || result.Message.Contains("inactive") || result.Message.Contains("Invalid refresh token") || result.Message.Contains("Invalid user")))
                {
                    return Unauthorized(new { success = false, status = "error", message = result.Message, errors = result.Errors });
                }
                
                return BadRequest(new { success = false, status = "error", message = result.Message, errors = result.Errors });
            }
            if (result.Data == null && (result.Message == "Success" || string.IsNullOrEmpty(result.Message)))
            {
                return Ok(new { success = true, status = "success" });
            }
            return Ok(new { success = true, status = "success", message = result.Message, data = result.Data });
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> RegisterAsync([FromBody] UserRegisterRequest request, CancellationToken ct)
        {
            var result = await _authService.RegisterAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            if (!result.Success) return HandleResult(result);
            return StatusCode(201, new { success = true, status = "success", message = result.Message, data = result.Data });
        }

        [HttpPost("check-availability")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> CheckAvailabilityAsync([FromBody] CheckAvailabilityRequest request, CancellationToken ct)
        {
            var result = await _authService.CheckAvailabilityAsync(request, ct);
            return HandleResult(result);
        }

        [HttpPost("send-register-otp")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("OtpRateThrottle")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> SendRegisterOtpAsync([FromBody] SendOtpRequest request, CancellationToken ct)
        {
            var result = await _authService.SendRegisterOtpAsync(request, ct);
            return HandleResult(result);
        }

        [HttpPost("verify-register-otp")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("OtpRateThrottle")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> VerifyRegisterOtpAsync([FromBody] VerifyOtpRequest request, CancellationToken ct)
        {
            var result = await _authService.VerifyRegisterOtpAsync(request, ct);
            return HandleResult(result);
        }

        [HttpPost("send-otp")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("OtpRateThrottle")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> SendOtpAsync([FromBody] SendOtpRequest request, CancellationToken ct)
        {
            var result = await _authService.SendOtpAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Path, Request.Method, ct);
            return HandleResult(result);
        }

        [HttpPost("verify-otp")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("OtpRateThrottle")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> VerifyOtpAsync([FromBody] VerifyOtpRequest request, CancellationToken ct)
        {
            var result = await _authService.VerifyOtpAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Path, Request.Method, ct);
            return HandleResult(result);
        }

        [HttpPost("login/email")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> LoginEmailAsync([FromBody] LoginEmailRequest request, CancellationToken ct)
        {
            var result = await _authService.LoginEmailAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Path, Request.Method, ct);
            return HandleResult(result);
        }

        [HttpPost("login/mobile")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> LoginMobileAsync([FromBody] LoginMobileRequest request, CancellationToken ct)
        {
            var result = await _authService.LoginMobileAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Path, Request.Method, ct);
            return HandleResult(result);
        }

        [HttpPost("login/admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> AdminLoginAsync([FromBody] AdminLoginRequest request, CancellationToken ct)
        {
            var result = await _authService.AdminLoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Path, Request.Method, ct);
            return HandleResult(result);
        }

        [HttpPost("forgot-password")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("OtpRateThrottle")]
        public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequest request, CancellationToken ct)
        {
            var result = await _authService.ForgotPasswordAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Path, Request.Method, ct);
            return HandleResult(result);
        }

        [HttpPost("forgot-password/verify")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("OtpRateThrottle")]
        public async Task<IActionResult> VerifyForgotPasswordOtpAsync([FromBody] VerifyResetOtpRequest request, CancellationToken ct)
        {
            var result = await _authService.VerifyForgotPasswordOtpAsync(request, ct);
            return HandleResult(result);
        }

        [HttpPost("reset-password")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("OtpRateThrottle")]
        public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequest request, CancellationToken ct)
        {
            var result = await _authService.ResetPasswordAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Path, Request.Method, ct);
            return HandleResult(result);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> LogoutAsync([FromBody] LogoutRequest request, CancellationToken ct)
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var currentUserIdStr = User.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
            var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            var result = await _authService.LogoutAsync(request, authHeader, currentUserIdStr, role, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Path, Request.Method, ct);
            return HandleResult(result);
        }

        [HttpPost("token/refresh")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> RefreshTokenAsync([FromBody] TokenRefreshRequest request, CancellationToken ct)
        {
            var result = await _authService.RefreshTokenAsync(request, ct);
            return HandleResult(result);
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request, CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("user_id");
            var result = await _authService.ChangePasswordAsync(request, userIdStr, ct);
            return HandleResult(result);
        }

        [HttpPost("fcm-token/update")]
        [Authorize]
        public async Task<IActionResult> UpdateFcmTokenAsync([FromBody] FcmTokenUpdateDto dto, CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("user_id");
            if (!long.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }
            var result = await _authService.UpdateFcmTokenAsync(dto, userId, ct);
            return HandleResult(result);
        }
    }

    public class CheckAvailabilityRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("email")]
        public string Email { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("mobile")]
        public string Mobile { get; set; }
    }

    public class AdminLoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LoginEmailRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("email")]
        public string Email { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("password")]
        public string Password { get; set; }
    }

    public class LoginMobileRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("mobile")]
        public string Mobile { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("password")]
        public string Password { get; set; }
    }

    public class UserRegisterRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("first_name")]
        public string FirstName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("last_name")]
        public string LastName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("email")]
        public string Email { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("mobile")]
        public string Mobile { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("password")]
        public string Password { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("confirm_password")]
        public string ConfirmPassword { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("goal")]
        public string Goal { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("dob")]
        public System.DateTime Dob { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("address")]
        public string Address { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("parent_mobile")]
        public string ParentMobile { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("otp")]
        public string Otp { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("gender")]
        public string Gender { get; set; }
    }

    public class SendOtpRequest
    {
        public string Mobile { get; set; }
    }

    public class VerifyOtpRequest
    {
        public string Mobile { get; set; }
        public string Otp { get; set; }
    }

    public class ForgotPasswordRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("identifier")]
        public string Identifier { get; set; }
    }

    public class VerifyResetOtpRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("identifier")]
        public string Identifier { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("token")]
        public string Token { get; set; }
    }

    public class ResetPasswordRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("identifier")]
        public string Identifier { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("token")]
        public string Token { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("new_password")]
        public string NewPassword { get; set; }
    }

    public class LogoutRequest
    {
        public string Refresh { get; set; }
    }

    public class TokenRefreshRequest
    {
        public string Refresh { get; set; }
    }

    public class ChangePasswordRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("old_password")]
        public string OldPassword { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("new_password")]
        public string NewPassword { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("confirm_password")]
        public string ConfirmPassword { get; set; }
    }
}
