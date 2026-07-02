using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Controllers;

namespace WebApplication1.Services
{
    public interface ILibraryService
    {
        Task<ServiceResult<object>> GetLibraryInfoAsync(string mediaBaseUrl, CancellationToken ct = default);
        Task<ServiceResult<object>> GetFacilitiesAsync(string mediaBaseUrl, CancellationToken ct = default);
        Task<ServiceResult<object>> GetAchieversAsync(bool? featured, string mediaBaseUrl, CancellationToken ct = default);
        Task<ServiceResult<object>> GetReviewsAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetReviewsSummaryAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> SubmitReviewAsync(long userId, LibraryController.SubmitReviewRequest request, CancellationToken ct = default);
        Task<ServiceResult<object>> GetSlidersAsync(string mediaBaseUrl, CancellationToken ct = default);
    }
}
