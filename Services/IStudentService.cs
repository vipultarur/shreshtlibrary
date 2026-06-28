using System.Threading.Tasks;
using WebApplication1.Models.Responses;
using WebApplication1.Models.DTOs.Student;

namespace WebApplication1.Services
{
    public interface IStudentService
    {
        Task<ApiResponse<object>?> GetProfileAsync(long userId, string scheme, string host, CancellationToken ct = default);
        Task<ApiResponse<object>?> UpdateProfileAsync(long userId, UpdateProfileDto dto, CancellationToken ct = default);
        Task<ApiResponse<object>?> GetDashboardAsync(long userId, CancellationToken ct = default);
        Task<ApiResponse<object>?> GetIdCardAsync(long userId, string scheme, string host, CancellationToken ct = default);
        Task<ApiResponse<object>> UploadPhotoAsync(long userId, Microsoft.AspNetCore.Http.IFormFile profile_photo, string scheme, string host, CancellationToken ct = default);
        Task<ApiResponse<object>> GetReferralCodeAsync(long userId, CancellationToken ct = default);
        Task<ApiResponse<object>> GenerateReferralCodeAsync(long userId, CancellationToken ct = default);
        Task<ApiResponse<object>> ApplyReferralAsync(long userId, string code, CancellationToken ct = default);
        Task<ApiResponse<object>> GetReferralHistoryAsync(long userId, CancellationToken ct = default);
    }
}
