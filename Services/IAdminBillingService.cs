using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Controllers;

namespace WebApplication1.Services
{
    public interface IAdminBillingService
    {
        Task<ServiceResult<object>> GetPlanStatsAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetAllPlansAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> CreatePlanAsync(AdminBillingController.PlanPayload payload, CancellationToken ct = default);
        Task<ServiceResult<object>> GetPlanDetailAsync(long id, CancellationToken ct = default);
        Task<ServiceResult<object>> UpdatePlanAsync(long id, AdminBillingController.PlanPayload payload, CancellationToken ct = default);
        Task<ServiceResult<object>> TogglePlanAsync(long id, AdminBillingController.TogglePayload payload, CancellationToken ct = default);
        Task<ServiceResult<object>> DeletePlanAsync(long id, CancellationToken ct = default);
        Task<ServiceResult<object>> GetPlanStudentsAsync(long id, CancellationToken ct = default);

        Task<ServiceResult<object>> AssignMembershipAsync(AdminBillingController.MembershipAssignPayload payload, CancellationToken ct = default);
        Task<ServiceResult<object>> GetMembershipsListAsync(int page, int pageSize, string search, string status, long? studentId, string nextTemplate, string prevTemplate, CancellationToken ct = default);

        Task<ServiceResult<object>> GetMembershipDetailAsync(long id, CancellationToken ct = default);
        Task<ServiceResult<object>> GetExpiringMembershipsAsync(int days, CancellationToken ct = default);
        Task<ServiceResult<object>> GetExpiredTodayMembershipsAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> RenewMembershipAsync(AdminBillingController.MembershipAssignPayload payload, CancellationToken ct = default);
        Task<ServiceResult<object>> UpgradeMembershipAsync(AdminBillingController.MembershipAssignPayload payload, CancellationToken ct = default);
        
        Task<ServiceResult<object>> GetPaymentsSummaryAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetPendingPaymentsAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetOverduePaymentsAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetPaymentsListAsync(int page, int pageSize, string search, string status, string nextTemplate, string prevTemplate, CancellationToken ct = default);
        Task<ServiceResult<object>> CreatePaymentAsync(AdminBillingController.PaymentCreatePayload payload, CancellationToken ct = default);
        Task<ServiceResult<object>> GetPaymentDetailAsync(long id, CancellationToken ct = default);
        Task<ServiceResult<object>> VerifyPaymentAsync(long id, CancellationToken ct = default);
        Task<ServiceResult<object>> RefundPaymentAsync(long id, AdminBillingController.RefundPayload payload, CancellationToken ct = default);
        Task<ServiceResult<object>> UpdatePaymentAsync(long id, AdminBillingController.PaymentUpdateDto payload, CancellationToken ct = default);
        Task<ServiceResult<object>> GetPaymentReceiptPdfAsync(long id, CancellationToken ct = default);
        Task<ServiceResult<object>> SendPaymentReceiptEmailAsync(long id, CancellationToken ct = default);
    }
}
