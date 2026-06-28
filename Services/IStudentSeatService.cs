using System.Threading;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public interface IStudentSeatService
    {
        Task<ServiceResult<object>> GetSeatLayoutAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetSeatHistoryAsync(long studentId, CancellationToken ct = default);
    }
}
