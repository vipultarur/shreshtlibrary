using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;
using WebApplication1.Models;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Threading;

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

        [HttpPost("register")]
        public Task<IActionResult> RegisterAsync([FromBody] UserRegisterRequest request, CancellationToken ct)
        {
            return _authService.RegisterAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        }

        [HttpPost("send-otp")]
        public Task<IActionResult> SendOtpAsync([FromBody] SendOtpRequest request, CancellationToken ct)
        {
            return _authService.SendOtpAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Path, Request.Method, ct);
        }

        [HttpPost("verify-otp")]
        public Task<IActionResult> VerifyOtpAsync([FromBody] VerifyOtpRequest request, CancellationToken ct)
        {
            return _authService.VerifyOtpAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Path, Request.Method, ct);
        }

        [HttpPost("login/email")]
        public Task<IActionResult> LoginEmailAsync([FromBody] LoginEmailRequest request, CancellationToken ct)
        {
            return _authService.LoginEmailAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Path, Request.Method, ct);
        }

        [HttpPost("login/mobile")]
        public Task<IActionResult> LoginMobileAsync([FromBody] LoginMobileRequest request, CancellationToken ct)
        {
            return _authService.LoginMobileAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Path, Request.Method, ct);
        }

        [HttpPost("login/admin")]
        public Task<IActionResult> AdminLoginAsync([FromBody] AdminLoginRequest request, CancellationToken ct)
        {
            return _authService.AdminLoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Path, Request.Method, ct);
        }

        [HttpPost("forgot-password")]
        public Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequest request, CancellationToken ct)
        {
            return _authService.ForgotPasswordAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Path, Request.Method, ct);
        }

        [HttpPost("reset-password")]
        public Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequest request, CancellationToken ct)
        {
            return _authService.ResetPasswordAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Path, Request.Method, ct);
        }

        [HttpPost("logout")]
        [Authorize]
        public Task<IActionResult> LogoutAsync([FromBody] LogoutRequest request, CancellationToken ct)
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var currentUserIdStr = User.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
            var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            return _authService.LogoutAsync(request, authHeader, currentUserIdStr, role, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Path, Request.Method, ct);
        }

        [HttpPost("token/refresh")]
        public Task<IActionResult> RefreshTokenAsync([FromBody] TokenRefreshRequest request, CancellationToken ct)
        {
            return _authService.RefreshTokenAsync(request, ct);
        }

        [HttpPost("change-password")]
        [Authorize]
        public Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request, CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("user_id");
            return _authService.ChangePasswordAsync(request, userIdStr, ct);
        }

        public class FcmTokenUpdateDto
        {
            public string Token { get; set; }
        }

        [HttpPost("fcm-token/update")]
        [Authorize]
        public Task<IActionResult> UpdateFcmTokenAsync([FromBody] FcmTokenUpdateDto dto, CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("user_id");
            if (!long.TryParse(userIdStr, out var userId))
            {
                return Task.FromResult<IActionResult>(Unauthorized());
            }
            return _authService.UpdateFcmTokenAsync(dto, userId, ct);
        }
    }

    public class AdminLoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LoginEmailRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginMobileRequest
    {
        public string Mobile { get; set; }
        public string Password { get; set; }
    }

    public class UserRegisterRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string Goal { get; set; }
        public System.DateTime Dob { get; set; }
        public string Caste { get; set; }
        public string Address { get; set; }
        public string ParentMobile { get; set; }
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
        public string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Token { get; set; }
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
