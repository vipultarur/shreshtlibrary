using System.Threading;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public interface IAdminSeatService
    {
        Task<ServiceResult<object>> GetSeatsLayoutAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> ReleaseAllSeatsAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> ReserveBulkSeatsAsync(System.Collections.Generic.List<int> seatIds, bool isReservedForGirls, CancellationToken ct = default);
        Task<ServiceResult<object>> GetAvailableSeatsAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetSeatsStatsAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> AddSeatAsync(string floor, string row, string seatNumber, string? status, string? notes, bool? isReservedForGirls, long? rowRefId, CancellationToken ct = default);
        Task<ServiceResult<bool>> DeleteSeatAsync(long pk, CancellationToken ct = default);
        Task<ServiceResult<object>> GetSeatsListAsync(int page = 1, int pageSize = 200, string nextTemplate = "", string prevTemplate = "", CancellationToken ct = default);
        Task<ServiceResult<object>> UpdateSeatAsync(long pk, string? floor, string? row, string? seatNumber, string? status, string? notes, bool? isReservedForGirls, long? rowRefId, CancellationToken ct = default);
        Task<ServiceResult<object>> UpdateSeatStatusAsync(long pk, string status, string? reason, CancellationToken ct = default);
        Task<ServiceResult<object>> AssignSeatAsync(long pk, long studentId, CancellationToken ct = default);
        Task<ServiceResult<object>> UnassignSeatAsync(long pk, string? reason, CancellationToken ct = default);
        Task<ServiceResult<object>> GetFloorsListAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> AddFloorAsync(string name, string? description, int order, CancellationToken ct = default);
        Task<ServiceResult<bool>> DeleteFloorAsync(long pk, CancellationToken ct = default);
        Task<ServiceResult<object>> GetRowsListAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> AddRowAsync(long floorId, string label, int order, CancellationToken ct = default);
        Task<ServiceResult<bool>> DeleteRowAsync(long pk, CancellationToken ct = default);
        Task<ServiceResult<object>> UpdateFloorAsync(long pk, string name, string? description, int order, CancellationToken ct = default);
        Task<ServiceResult<object>> UpdateRowAsync(long pk, long floorId, string label, int order, CancellationToken ct = default);
        Task<ServiceResult<object>> GetSeatDetailAsync(long pk, CancellationToken ct = default);
        Task<ServiceResult<object>> GetSeatHistoryAsync(long pk, CancellationToken ct = default);
        Task<ServiceResult<object>> GetFloorDetailAsync(long pk, CancellationToken ct = default);
        Task<ServiceResult<object>> GetRowDetailAsync(long pk, CancellationToken ct = default);
    }
}
