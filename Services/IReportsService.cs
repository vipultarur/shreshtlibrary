using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public interface IReportsService
    {
        Task<ServiceResult<object>> GetAttendanceReportAsync(int page, int pageSize, CancellationToken ct = default);
        Task<ServiceResult<object>> GetPaymentsReportAsync(int page, int pageSize, CancellationToken ct = default);
        Task<ServiceResult<object>> GetStudentsReportAsync(int page, int pageSize, CancellationToken ct = default);
        Task<ServiceResult<object>> GetMembershipsReportAsync(int page, int pageSize, CancellationToken ct = default);
        Task<ServiceResult<object>> GetDailySummaryAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetSeatsReportAsync(CancellationToken ct = default);
        Task<byte[]> ExportReportCsvAsync(string kind, CancellationToken ct = default);
    }
}
