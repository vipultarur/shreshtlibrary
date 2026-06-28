using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Controllers; // To access StudentPayload for now

namespace WebApplication1.Services
{
    public interface IStudentAdminService
    {
        Task<ServiceResult<object>> GetStudentCountsAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetStudentsAsync(int page, int pageSize, string search, string status, string scheme, string host, CancellationToken ct = default);
        Task<ServiceResult<object>> GetStudentDetailAsync(string pk, string scheme, string host, CancellationToken ct = default);
        Task<ServiceResult<object>> CreateStudentAsync(AdminStudentsController.StudentPayload payload, CancellationToken ct = default);
        Task<ServiceResult<object>> UpdateStudentAsync(string pk, AdminStudentsController.StudentPayload payload, CancellationToken ct = default);
        Task<ServiceResult<bool>> DeleteStudentAsync(string pk, CancellationToken ct = default);
        Task<ServiceResult<object>> GetStudentAnalyticsAsync(string pk, CancellationToken ct = default);
        Task<ServiceResult<object>> SuspendStudentAsync(string pk, string? reason, CancellationToken ct = default);
        Task<ServiceResult<object>> ActivateStudentAsync(string pk, CancellationToken ct = default);
        Task<ServiceResult<object>> GetStudentRelatedDataAsync(string pk, string kind, CancellationToken ct = default);
        Task<ServiceResult<object>> UploadStudentPhotoAsync(string pk, Microsoft.AspNetCore.Http.IFormFile photo, string scheme, string host, CancellationToken ct = default);
        Task<ServiceResult<object>> ExportStudentsAsync(CancellationToken ct = default);
    }
}
