using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebApplication1.DTOs.Admin;

namespace WebApplication1.Services
{
    public interface IAdminDashboardService
    {
        Task<object?> GetAdminProfileAsync(long userId, CancellationToken ct);
        Task<object?> UpdateAdminProfileAsync(long userId, AdminProfileUpdateDto request, string scheme, string host, CancellationToken ct);
        Task<object> GetStatsOverviewAsync(string section, CancellationToken ct);
        Task<object> GetDashboardChartsAsync(string range, CancellationToken ct);
        Task<object> GetAdminInboxAsync(CancellationToken ct);
        Task<object> GetDashboardAlertsAsync(CancellationToken ct);
        Task<object> GetRecentActivityAsync(CancellationToken ct);
        Task<object> GetAttendanceOverviewChartsAsync(CancellationToken ct);
        Task<object> GetRevenueOverviewChartsAsync(CancellationToken ct);
        Task<object> GetStudentsOverviewChartsAsync(CancellationToken ct);
        Task<object> GetMembershipsOverviewChartsAsync(CancellationToken ct);
    }
}
