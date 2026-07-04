using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers;
using WebApplication1.Models.DTOs.Auth;

namespace WebApplication1.Services
{
    public interface IAuthService
    {
        Task<ServiceResult<object>> RegisterAsync(UserRegisterRequest request, string ipAddress, CancellationToken ct = default);
        Task<ServiceResult<object>> SendOtpAsync(SendOtpRequest request, string ipAddress, string path, string method, CancellationToken ct = default);
        Task<ServiceResult<object>> VerifyOtpAsync(VerifyOtpRequest request, string ipAddress, string path, string method, CancellationToken ct = default);
        Task<ServiceResult<object>> LoginEmailAsync(LoginEmailRequest request, string ipAddress, string path, string method, CancellationToken ct = default);
        Task<ServiceResult<object>> LoginMobileAsync(LoginMobileRequest request, string ipAddress, string path, string method, CancellationToken ct = default);
        Task<ServiceResult<object>> AdminLoginAsync(AdminLoginRequest request, string ipAddress, string path, string method, CancellationToken ct = default);
        Task<ServiceResult<object>> ForgotPasswordAsync(ForgotPasswordRequest request, string ipAddress, string path, string method, CancellationToken ct = default);
        Task<ServiceResult<object>> ResetPasswordAsync(ResetPasswordRequest request, string ipAddress, string path, string method, CancellationToken ct = default);
        Task<ServiceResult<object>> LogoutAsync(LogoutRequest request, string authHeader, string currentUserIdStr, string role, string ipAddress, string path, string method, CancellationToken ct = default);
        Task<ServiceResult<object>> RefreshTokenAsync(TokenRefreshRequest request, CancellationToken ct = default);
        Task<ServiceResult<object>> ChangePasswordAsync(ChangePasswordRequest request, string userIdStr, CancellationToken ct = default);
        Task<ServiceResult<object>> UpdateFcmTokenAsync(FcmTokenUpdateDto dto, long userId, CancellationToken ct = default);
    }
}
