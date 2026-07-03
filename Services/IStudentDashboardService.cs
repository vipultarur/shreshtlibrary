using System.Threading.Tasks;
using System.Threading;
using WebApplication1.Models.Responses;

namespace WebApplication1.Services
{
    public interface IStudentDashboardService
    {
        Task<ApiResponse<object>?> GetDashboardAsync(long userId, CancellationToken ct = default);
    }
}
