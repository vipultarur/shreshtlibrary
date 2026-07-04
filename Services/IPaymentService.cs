using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Controllers;
using WebApplication1.Models.DTOs.Billing;

namespace WebApplication1.Services
{
    public interface IPaymentService
    {
        Task<ServiceResult<object>> GetPublicPlansAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetMembershipPlansAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetMembershipHistoryAsync(long studentId, CancellationToken ct = default);
        Task<ServiceResult<object>> InitiatePaymentAsync(long studentId, InitiatePaymentPayload payload, CancellationToken ct = default);
        Task<ServiceResult<object>> GetPaymentHistoryAsync(long studentId, CancellationToken ct = default);
    }
}
