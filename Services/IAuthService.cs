using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers;

namespace WebApplication1.Services
{
    public interface IAuthService
    {
        Task<IActionResult> RegisterAsync(UserRegisterRequest request, string ipAddress, CancellationToken ct = default);
        Task<IActionResult> SendOtpAsync(SendOtpRequest request, string ipAddress, string path, string method, CancellationToken ct = default);
        Task<IActionResult> VerifyOtpAsync(VerifyOtpRequest request, string ipAddress, string path, string method, CancellationToken ct = default);
        Task<IActionResult> LoginEmailAsync(LoginEmailRequest request, string ipAddress, string path, string method, CancellationToken ct = default);
        Task<IActionResult> LoginMobileAsync(LoginMobileRequest request, string ipAddress, string path, string method, CancellationToken ct = default);
        Task<IActionResult> AdminLoginAsync(AdminLoginRequest request, string ipAddress, string path, string method, CancellationToken ct = default);
        Task<IActionResult> ForgotPasswordAsync(ForgotPasswordRequest request, string ipAddress, string path, string method, CancellationToken ct = default);
        Task<IActionResult> ResetPasswordAsync(ResetPasswordRequest request, string ipAddress, string path, string method, CancellationToken ct = default);
        Task<IActionResult> LogoutAsync(LogoutRequest request, string authHeader, string currentUserIdStr, string role, string ipAddress, string path, string method, CancellationToken ct = default);
        Task<IActionResult> RefreshTokenAsync(TokenRefreshRequest request, CancellationToken ct = default);
        Task<IActionResult> ChangePasswordAsync(ChangePasswordRequest request, string userIdStr, CancellationToken ct = default);
        Task<IActionResult> UpdateFcmTokenAsync(AuthController.FcmTokenUpdateDto dto, long userId, CancellationToken ct = default);
    }
}
