using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Models.DTOs;
using WebApplication1.Models.Responses;

namespace WebApplication1.Services
{
    public interface IAdminSettingsService
    {
        Task<ServiceResult<object>> GetSettingsAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> UpdateSettingsAsync(SettingsPayload payload, CancellationToken ct = default);
    }
}
