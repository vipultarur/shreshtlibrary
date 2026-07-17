using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public class PlatformPlanPayload
    {
        public string Name { get; set; } = "";
        public decimal MonthlyPrice { get; set; }
        public decimal QuarterlyPrice { get; set; }
        public decimal HalfYearlyPrice { get; set; }
        public decimal YearlyPrice { get; set; }
        public int MaxStudents { get; set; }
        public int MaxStaff { get; set; }
        public string[] Features { get; set; } = Array.Empty<string>();
        public bool IsRecommended { get; set; }
    }

    public class PlatformPaymentSettingsPayload
    {
        public string MerchantName { get; set; } = "";
        public string UpiId { get; set; } = "";
        public string? QrCodePath { get; set; }
        public string? BankAccount { get; set; }
        public string? AccountHolder { get; set; }
        public string? Ifsc { get; set; }
        public string? PaymentInstructions { get; set; }
    }

    public class LibraryPaymentSubmitPayload
    {
        public long PlanId { get; set; }
        public int DurationDays { get; set; }
        public decimal Amount { get; set; }
        public string UtrNumber { get; set; } = "";
        public string? ScreenshotPath { get; set; }
    }

    public interface IAdminLicensingService
    {
        Task<ServiceResult<object>> GetPlatformPlansAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> CreatePlatformPlanAsync(PlatformPlanPayload payload, CancellationToken ct = default);
        Task<ServiceResult<object>> DeletePlatformPlanAsync(long id, CancellationToken ct = default);
        Task<ServiceResult<object>> GetPlatformPaymentSettingsAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> UpdatePlatformPaymentSettingsAsync(PlatformPaymentSettingsPayload payload, CancellationToken ct = default);
        
        Task<ServiceResult<object>> GetLibraryPaymentsAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> ApproveLibraryPaymentAsync(long paymentId, long adminId, CancellationToken ct = default);
        
        Task<ServiceResult<object>> GetCurrentSubscriptionAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> SubmitLibraryPaymentAsync(LibraryPaymentSubmitPayload payload, CancellationToken ct = default);
    }
}
