using System.Threading.Tasks;
using System.Threading;
using WebApplication1.Models.Responses;

namespace WebApplication1.Services
{
    public interface IStudentReferralService
    {
        Task<ApiResponse<object>> GetReferralCodeAsync(long userId, CancellationToken ct = default);
        Task<ApiResponse<object>> GenerateReferralCodeAsync(long userId, CancellationToken ct = default);
        Task<ApiResponse<object>> ApplyReferralAsync(long userId, string code, CancellationToken ct = default);
        Task<ApiResponse<object>> GetReferralHistoryAsync(long userId, CancellationToken ct = default);
    }
}
