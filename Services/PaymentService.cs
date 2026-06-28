using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Controllers;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;

        public PaymentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<object>> GetPublicPlansAsync(CancellationToken ct = default)
        {
            var plans = await _context.MembershipsMembershipplans
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    duration_months = p.DurationMonths,
                    duration_days = p.DurationDays,
                    price = p.Price,
                    description = p.Description,
                    is_active = p.IsActive,
                    benefits = p.Benefits,
                    sort_order = p.SortOrder
                })
                .ToListAsync(ct);

            var results = plans.Select(p => new {
                id = p.id,
                name = p.name,
                duration_months = p.duration_months,
                duration_days = p.duration_days,
                price = p.price,
                description = p.description,
                is_active = p.is_active,
                benefits = string.IsNullOrWhiteSpace(p.benefits) ? Array.Empty<string>() : p.benefits.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
                sort_order = p.sort_order
            });

            return ServiceResult<object>.Ok(results);
        }

        public async Task<ServiceResult<object>> GetMembershipPlansAsync(CancellationToken ct = default)
        {
            return await GetPublicPlansAsync(ct);
        }

        public async Task<ServiceResult<object>> GetMembershipHistoryAsync(long studentId, CancellationToken ct = default)
        {
            var memberships = await _context.MembershipsMemberships
                .AsNoTracking()
                .Include(m => m.Plan)
                .Where(m => m.StudentId == studentId)
                .OrderByDescending(m => m.StartDate)
                .Select(m => new {
                    id = m.Id,
                    plan = new { id = m.Plan.Id, name = m.Plan.Name },
                    plan_name_snapshot = m.PlanNameSnapshot,
                    price_snapshot = m.PriceSnapshot.ToString(),
                    start_date = m.StartDate.ToString("yyyy-MM-dd"),
                    end_date = m.EndDate.ToString("yyyy-MM-dd"),
                    status = m.Status,
                    created_at = m.CreatedAt.HasValue ? m.CreatedAt.Value.ToString("O") : null
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(memberships);
        }

        public async Task<ServiceResult<object>> InitiatePaymentAsync(long studentId, BillingController.InitiatePaymentPayload payload, CancellationToken ct = default)
        {
            var plan = await _context.MembershipsMembershipplans.FindAsync(new object[] { (long)payload.plan_id }, ct);
            if (plan == null || !plan.IsActive) 
                return ServiceResult<object>.Fail("Plan ID is required or inactive.");

            var activeSub = await _context.MembershipsMemberships.FirstOrDefaultAsync(m => m.StudentId == studentId && m.Status == "active", ct);
            if (activeSub != null)
                return ServiceResult<object>.Fail("You already have an active subscription. You can purchase a new plan after the current one expires.");

            var baseDuration = plan.DurationDays > 0 ? plan.DurationDays : 30;
            var pricePerDay = plan.Price / baseDuration;
            var amount = Math.Round(pricePerDay * payload.duration_days, 2);

            using var transaction = await _context.Database.BeginTransactionAsync(ct);
            PaymentsPayment payment;
            try
            {
                var membership = new MembershipsMembership
                {
                    StudentId = studentId,
                    PlanId = plan.Id,
                    PlanNameSnapshot = plan.Name,
                    PriceSnapshot = plan.Price,
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(payload.duration_days)),
                    Status = "pending", // Pending verification
                    CreatedAt = DateTime.UtcNow
                };
                _context.MembershipsMemberships.Add(membership);
                await _context.SaveChangesAsync(ct);

                payment = new PaymentsPayment
                {
                    PaymentId = $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                    StudentId = studentId,
                    MembershipId = membership.Id,
                    Amount = amount,
                    Status = "pending",
                    Method = "ONLINE",
                    PaymentMode = payload.payment_mode,
                    TransactionId = payload.transaction_id,
                    PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    CreatedAt = DateTime.UtcNow
                };
                _context.PaymentsPayments.Add(payment);
                await _context.SaveChangesAsync(ct);
                
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }

            return ServiceResult<object>.Ok(
                new { id = payment.Id, status = payment.Status, amount = payment.Amount },
                "Payment transaction initiated successfully. Pending admin approval."
            );
        }

        public async Task<ServiceResult<object>> GetPaymentHistoryAsync(long studentId, CancellationToken ct = default)
        {
            var payments = await _context.PaymentsPayments
                .AsNoTracking()
                .Include(p => p.Membership)
                    .ThenInclude(m => m!.Plan)
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new {
                    id = p.Id,
                    payment_id = p.PaymentId,
                    plan_name = p.Membership != null && p.Membership.Plan != null ? p.Membership.Plan.Name : null,
                    amount = p.Amount.ToString(),
                    status = p.Status,
                    payment_mode = p.PaymentMode,
                    payment_date = p.PaymentDate.ToString("yyyy-MM-dd"),
                    transaction_id = p.TransactionId
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(payments);
        }
    }
}
