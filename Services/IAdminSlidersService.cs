using System.Threading;
using System.Threading.Tasks;
using WebApplication1.DTOs.Admin;

namespace WebApplication1.Services
{
    public interface IAdminSlidersService
    {
        Task<ServiceResult<object>> GetSliders(CancellationToken ct = default);
        Task<ServiceResult<object>> CreateSlider(SliderDto dto, CancellationToken ct = default);
        Task<ServiceResult<object>> UpdateSlider(long id, SliderDto dto, CancellationToken ct = default);
        Task<ServiceResult<object>> DeleteSlider(long id, CancellationToken ct = default);
    }
}
