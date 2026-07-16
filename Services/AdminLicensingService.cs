using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class AdminLicensingService : IAdminLicensingService
    {
        private readonly ApplicationDbContext _context;

        public AdminLicensingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<object>> GetPlatformPlansAsync(CancellationToken ct = default)
        {
            var plans = await _context.PlatformSubscriptionPlans
                .OrderBy(p => p.DisplayOrder)
                .Select(p => new
                {
                    p.Id,
                    p.PlanName,
                    p.MonthlyPrice,
                    p.QuarterlyPrice,
                    p.HalfYearlyPrice,
                    p.YearlyPrice,
                    p.MaxStudents,
                    p.MaxStaff,
                    Features = JsonSerializer.Deserialize<string[]>(p.Features, (JsonSerializerOptions?)null) ?? Array.Empty<string>(),
                    p.IsActive,
                    p.IsRecommended
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(plans);
        }

        public async Task<ServiceResult<object>> CreatePlatformPlanAsync(PlatformPlanPayload payload, CancellationToken ct = default)
        {
            var plan = new PlatformSubscriptionPlan
            {
                PlanName = payload.Name,
                MonthlyPrice = payload.MonthlyPrice,
                QuarterlyPrice = payload.QuarterlyPrice,
                HalfYearlyPrice = payload.HalfYearlyPrice,
                YearlyPrice = payload.YearlyPrice,
                MaxStudents = payload.MaxStudents,
                MaxStaff = payload.MaxStaff,
                Features = JsonSerializer.Serialize(payload.Features),
                IsRecommended = payload.IsRecommended,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PlatformSubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(plan);
        }

        public async Task<ServiceResult<object>> GetPlatformPaymentSettingsAsync(CancellationToken ct = default)
        {
            var settings = await _context.PlatformPaymentSettings.FirstOrDefaultAsync(ct);
            return ServiceResult<object>.Ok(settings ?? new object());
        }

        public async Task<ServiceResult<object>> UpdatePlatformPaymentSettingsAsync(PlatformPaymentSettingsPayload payload, CancellationToken ct = default)
        {
            var settings = await _context.PlatformPaymentSettings.FirstOrDefaultAsync(ct);
            if (settings == null)
            {
                settings = new PlatformPaymentSetting();
                _context.PlatformPaymentSettings.Add(settings);
            }

            settings.MerchantName = payload.MerchantName;
            settings.UpiId = payload.UpiId;
            if (payload.QrCodePath != null) settings.QrCodePath = payload.QrCodePath;
            settings.BankAccount = payload.BankAccount;
            settings.AccountHolder = payload.AccountHolder;
            settings.Ifsc = payload.Ifsc;
            settings.PaymentInstructions = payload.PaymentInstructions;
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(settings);
        }

        public async Task<ServiceResult<object>> GetLibraryPaymentsAsync(CancellationToken ct = default)
        {
            var payments = await _context.LibraryPayments
                .Include(p => p.Plan)
                .OrderByDescending(p => p.SubmittedAt)
                .Select(p => new
                {
                    p.Id,
                    PlanName = p.Plan.PlanName,
                    p.Amount,
                    p.DurationDays,
                    p.UtrNumber,
                    p.ScreenshotPath,
                    p.Status,
                    p.SubmittedAt,
                    p.ApprovedAt
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(payments);
        }

        public async Task<ServiceResult<object>> ApproveLibraryPaymentAsync(long paymentId, long adminId, CancellationToken ct = default)
        {
            var payment = await _context.LibraryPayments
                .Include(p => p.Plan)
                .FirstOrDefaultAsync(p => p.Id == paymentId, ct);

            if (payment == null) return ServiceResult<object>.NotFound("Payment not found");
            if (payment.Status != "Pending") return ServiceResult<object>.Fail("Payment is not pending");

            payment.Status = "Approved";
            payment.ApprovedAt = DateTime.UtcNow;
            payment.ApprovedById = adminId;

            // Update or create subscription
            var latestSub = await _context.LibrarySubscriptions
                .OrderByDescending(s => s.ExpiryDate)
                .FirstOrDefaultAsync(ct);

            var startDate = DateTime.UtcNow;
            if (latestSub != null && latestSub.ExpiryDate > DateTime.UtcNow)
            {
                startDate = latestSub.ExpiryDate;
            }

            var newSub = new LibrarySubscription
            {
                PlanId = payment.PlanId,
                StartDate = startDate,
                ExpiryDate = startDate.AddDays(payment.DurationDays),
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.LibrarySubscriptions.Add(newSub);
            await _context.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok(newSub);
        }

        public async Task<ServiceResult<object>> GetCurrentSubscriptionAsync(CancellationToken ct = default)
        {
            var sub = await _context.LibrarySubscriptions
                .Include(s => s.Plan)
                .OrderByDescending(s => s.ExpiryDate)
                .FirstOrDefaultAsync(ct);

            if (sub == null) return ServiceResult<object>.Ok(new { Status = "None" });

            return ServiceResult<object>.Ok(new
            {
                sub.Id,
                sub.PlanId,
                PlanName = sub.Plan.PlanName,
                sub.StartDate,
                sub.ExpiryDate,
                sub.Status,
                IsExpired = sub.ExpiryDate < DateTime.UtcNow,
                Features = JsonSerializer.Deserialize<string[]>(sub.Plan.Features, (JsonSerializerOptions?)null) ?? Array.Empty<string>()
            });
        }

        public async Task<ServiceResult<object>> SubmitLibraryPaymentAsync(LibraryPaymentSubmitPayload payload, CancellationToken ct = default)
        {
            var payment = new LibraryPayment
            {
                PlanId = payload.PlanId,
                Amount = payload.Amount,
                DurationDays = payload.DurationDays,
                UtrNumber = payload.UtrNumber,
                ScreenshotPath = payload.ScreenshotPath,
                Status = "Pending",
                SubmittedAt = DateTime.UtcNow
            };

            _context.LibraryPayments.Add(payment);
            await _context.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok(new { payment.Id, payment.Status });
        }
    }
}
