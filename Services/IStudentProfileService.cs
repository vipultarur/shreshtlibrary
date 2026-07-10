using System.Threading.Tasks;
using System.Threading;
using WebApplication1.Models.Responses;
using WebApplication1.Models.DTOs.Student;

namespace WebApplication1.Services
{
    public interface IStudentProfileService
    {
        Task<ApiResponse<object>?> GetProfileAsync(long userId, string scheme, string host, CancellationToken ct = default);
        Task<ApiResponse<object>?> UpdateProfileAsync(long userId, UpdateProfileDto dto, string scheme, string host, CancellationToken ct = default);
        Task<ApiResponse<object>?> GetIdCardAsync(long userId, string scheme, string host, CancellationToken ct = default);
        Task<ApiResponse<object>> UploadPhotoAsync(long userId, Microsoft.AspNetCore.Http.IFormFile profile_photo, string scheme, string host, CancellationToken ct = default);
    }
}
