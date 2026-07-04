using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Models.DTOs.Study;

namespace WebApplication1.Services
{
    public interface IStudyService
    {
        Task<ServiceResult<object>> StartSessionAsync(long userId, CancellationToken ct = default);
        Task<ServiceResult<object>> EndSessionAsync(long userId, EndSessionRequest request, CancellationToken ct = default);
        Task<ServiceResult<object>> GetCurrentSessionAsync(long userId, CancellationToken ct = default);
        Task<ServiceResult<object>> UpdateSessionAsync(long userId, UpdateSessionRequest request, CancellationToken ct = default);
        Task<ServiceResult<object>> GetSessionHistoryAsync(long userId, int page, int pageSize, CancellationToken ct = default);
        Task<ServiceResult<object>> GetLeaderboardAsync(string duration, string? startDate, string? endDate, string mediaBaseUrl, CancellationToken ct = default);
    }
}
