using System.Threading;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public interface IAttendanceService
    {
        Task<object?> GetTodayQrAsync(CancellationToken ct);
        Task<object?> ScanQrAsync(long userId, string qrHash, CancellationToken ct);
        Task<object> GetAttendanceLogsAsync(long userId, CancellationToken ct);
        Task<object> GetHolidaysAsync(string? fromDate, string? toDate, string? date, bool? isActive, CancellationToken ct);
    }
}
