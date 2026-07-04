using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Models;
using WebApplication1.Models.Responses;
using WebApplication1.Models.DTOs.Admin;
using WebApplication1.Controllers;

namespace WebApplication1.Services
{
    public interface ISuperAdminService
    {
        Task<ServiceResult<object>> AddAdminAsync(AdminPayload payload, CancellationToken ct = default);
        Task<ServiceResult<object>> UpdateAdminAsync(long pk, AdminPayload payload, CancellationToken ct = default);
        Task<ServiceResult<object>> GetAdminsListAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetAdminDetailAsync(long pk, CancellationToken ct = default);
        Task<ServiceResult<object>> RemoveAdminAsync(long pk, CancellationToken ct = default);
        Task<ServiceResult<object>> DeactivateAdminAsync(long pk, CancellationToken ct = default);
        Task<ServiceResult<object>> GetPermissionsListAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> AssignPermissionsAsync(PermissionPayload payload, CancellationToken ct = default);
        Task<ServiceResult<object>> CreateBackupAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetBackupListAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> RestoreBackupAsync(string backupId, CancellationToken ct = default);
        Task<ServiceResult<object>> GetBackupDataAsync(string backupId, CancellationToken ct = default);
        Task<ServiceResult<object>> GetActivityLogAsync(int page, int pageSize, CancellationToken ct = default);
    }
}
