using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public interface IAdminAttendanceService
    {
        Task<ServiceResult<object>> GetCurrentQrAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetQrHistoryAsync(int page, int pageSize, CancellationToken ct = default);
        Task<ServiceResult<object>> GenerateQrAsync(string? expiryDuration, CancellationToken ct = default);
        Task<ServiceResult<object>> RegenerateQrAsync(string? expiryDuration, CancellationToken ct = default);
        Task<ServiceResult<bool>> ExpireAllQrAsync(CancellationToken ct = default);
        Task<ServiceResult<bool>> DeleteQrAsync(long pk, CancellationToken ct = default);
        Task<ServiceResult<bool>> ClearAllQrAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetQrScansAsync(long pk, CancellationToken ct = default);
        
        Task<ServiceResult<object>> GetHolidaysAsync(string? fromDate, string? toDate, string? date, bool? isActive, CancellationToken ct = default);
        Task<ServiceResult<object>> GetAttendanceDailySummaryAsync(string? date, CancellationToken ct = default);
        Task<ServiceResult<object>> GetAttendanceAbsenteesAsync(string? date, CancellationToken ct = default);
        Task<ServiceResult<object>> GetAttendanceStreakAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetAttendanceListAsync(string? date, string? fromDate, string? toDate, int page = 1, int pageSize = 100, string nextTemplate = "", string prevTemplate = "", CancellationToken ct = default);
        Task<ServiceResult<bool>> RecordManualAttendanceAsync(WebApplication1.Models.DTOs.Attendance.ManualAttendanceDto dto, CancellationToken ct = default);
        Task<ServiceResult<object>> RecordManualBulkAttendanceAsync(List<WebApplication1.Models.DTOs.Attendance.ManualAttendanceDto> dtos, CancellationToken ct = default);
        Task<ServiceResult<object>> GetHolidayDetailAsync(long pk, CancellationToken ct = default);
        Task<ServiceResult<object>> GetAttendanceDetailAsync(long pk, CancellationToken ct = default);
    }
}
