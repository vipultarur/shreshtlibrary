using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using WebApplication1.Controllers;
using WebApplication1.Data;
using WebApplication1.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WebApplication1.Services
{
    public class AdminBillingService : IAdminBillingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly WhatsAppNotificationService _whatsappService;

        public AdminBillingService(ApplicationDbContext context, IEmailService emailService, IServiceScopeFactory scopeFactory, WhatsAppNotificationService whatsappService)
        {
            _context = context;
            _emailService = emailService;
            _scopeFactory = scopeFactory;
            _whatsappService = whatsappService;
        }

        public async Task<ServiceResult<object>> GetPlanStatsAsync(CancellationToken ct = default)
        {
            var planStats = await _context.MembershipsMembershipplans
                .GroupBy(p => 1)
                .Select(g => new {
                    Total = g.Count(),
                    Active = g.Count(p => p.IsActive)
                }).FirstOrDefaultAsync(ct);

            var membershipStats = await _context.MembershipsMemberships
                .GroupBy(m => 1)
                .Select(g => new {
                    Total = g.Count(),
                    Active = g.Count(m => m.Status.ToLower() == "active")
                }).FirstOrDefaultAsync(ct);

            var activePlans = planStats?.Active ?? 0;
            var totalPlans = planStats?.Total ?? 0;
            var activeMemberships = membershipStats?.Active ?? 0;
            var totalMemberships = membershipStats?.Total ?? 0;

            return ServiceResult<object>.Ok(new { total_plans = totalPlans, active_plans = activePlans, total_memberships = totalMemberships, active_memberships = activeMemberships });
        }

        public async Task<ServiceResult<object>> GetAllPlansAsync(CancellationToken ct = default)
        {
            var plans = await _context.MembershipsMembershipplans
                .AsNoTracking()
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
                benefits = string.IsNullOrWhiteSpace(p.benefits) ? new System.Collections.Generic.List<string>() : System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(p.benefits),
                sort_order = p.sort_order
            });

            return ServiceResult<object>.Ok(results);
        }

        public async Task<ServiceResult<object>> CreatePlanAsync(AdminBillingController.PlanPayload payload, CancellationToken ct = default)
        {
            var plan = new MembershipsMembershipplan
            {
                Name = payload.name,
                DurationMonths = payload.duration_months,
                DurationDays = payload.duration_days ?? 0,
                Price = payload.price,
                Description = payload.description,
                IsActive = payload.is_active ?? true,
                Benefits = System.Text.Json.JsonSerializer.Serialize(payload.benefits ?? new System.Collections.Generic.List<string>()),
                SortOrder = payload.sort_order ?? 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.MembershipsMembershipplans.Add(plan);
            await _context.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok(new {
                id = plan.Id,
                name = plan.Name,
                duration_months = plan.DurationMonths,
                duration_days = plan.DurationDays,
                price = plan.Price,
                description = plan.Description,
                is_active = plan.IsActive,
                benefits = string.IsNullOrWhiteSpace(plan.Benefits) ? new System.Collections.Generic.List<string>() : System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(plan.Benefits),
                sort_order = plan.SortOrder
            });
        }

        public async Task<ServiceResult<object>> GetPlanDetailAsync(long id, CancellationToken ct = default)
        {
            var plan = await _context.MembershipsMembershipplans.FindAsync(new object[] { id }, ct);
            if (plan == null) return ServiceResult<object>.NotFound("Plan not found");
            
            return ServiceResult<object>.Ok(new {
                id = plan.Id,
                name = plan.Name,
                duration_months = plan.DurationMonths,
                duration_days = plan.DurationDays,
                price = plan.Price,
                description = plan.Description,
                is_active = plan.IsActive,
                benefits = string.IsNullOrWhiteSpace(plan.Benefits) ? new System.Collections.Generic.List<string>() : System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(plan.Benefits),
                sort_order = plan.SortOrder
            });
        }

        public async Task<ServiceResult<object>> UpdatePlanAsync(long id, AdminBillingController.PlanPayload payload, CancellationToken ct = default)
        {
            var plan = await _context.MembershipsMembershipplans.FindAsync(new object[] { id }, ct);
            if (plan == null) return ServiceResult<object>.NotFound("Plan not found");

            if (!string.IsNullOrEmpty(payload.name)) plan.Name = payload.name;
            if (payload.duration_months > 0) plan.DurationMonths = payload.duration_months;
            if (payload.duration_days.HasValue) plan.DurationDays = payload.duration_days.Value;
            if (payload.price > 0) plan.Price = payload.price;
            if (payload.description != null) plan.Description = payload.description;
            if (payload.is_active.HasValue) plan.IsActive = payload.is_active.Value;
            if (payload.benefits != null) plan.Benefits = System.Text.Json.JsonSerializer.Serialize(payload.benefits);
            if (payload.sort_order.HasValue) plan.SortOrder = payload.sort_order.Value;

            plan.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok(new {
                id = plan.Id,
                name = plan.Name,
                duration_months = plan.DurationMonths,
                duration_days = plan.DurationDays,
                price = plan.Price,
                description = plan.Description,
                is_active = plan.IsActive,
                benefits = string.IsNullOrWhiteSpace(plan.Benefits) ? new System.Collections.Generic.List<string>() : System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(plan.Benefits),
                sort_order = plan.SortOrder
            });
        }

        public async Task<ServiceResult<object>> TogglePlanAsync(long id, AdminBillingController.TogglePayload payload, CancellationToken ct = default)
        {
            var plan = await _context.MembershipsMembershipplans.FindAsync(new object[] { id }, ct);
            if (plan == null) return ServiceResult<object>.NotFound("Plan not found");

            if (payload.is_active.HasValue)
                plan.IsActive = payload.is_active.Value;
            else
                plan.IsActive = !plan.IsActive;

            plan.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok(new {
                id = plan.Id,
                name = plan.Name,
                duration_months = plan.DurationMonths,
                duration_days = plan.DurationDays,
                price = plan.Price,
                description = plan.Description,
                is_active = plan.IsActive,
                benefits = string.IsNullOrWhiteSpace(plan.Benefits) ? new System.Collections.Generic.List<string>() : System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(plan.Benefits),
                sort_order = plan.SortOrder
            });
        }

        public async Task<ServiceResult<object>> DeletePlanAsync(long id, CancellationToken ct = default)
        {
            try {
                var plan = await _context.MembershipsMembershipplans.FindAsync(new object[] { id }, ct);
                if (plan == null) return ServiceResult<object>.NotFound("Plan not found");

                _context.MembershipsMembershipplans.Remove(plan);
                await _context.SaveChangesAsync(ct);

                return ServiceResult<object>.Ok(new { });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException) {
                return ServiceResult<object>.Fail("Cannot delete this plan because it is assigned to students.");
            }
        }

        public async Task<ServiceResult<object>> GetPlanStudentsAsync(long id, CancellationToken ct = default)
        {
            var students = await _context.StudentsStudentprofiles
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => _context.MembershipsMemberships
                    .Any(m => m.StudentId == s.UserId && m.PlanId == id))
                .Select(s => new {
                    id = s.Id,
                    user_id = s.UserId,
                    student_id = s.StudentId,
                    username = s.User.Username,
                    first_name = s.User.FirstName,
                    middle_name = s.MiddleName,
                    last_name = s.User.LastName,
                    email = s.User.Email,
                    mobile = s.User.Mobile,
                    is_active = s.User.IsActive,
                    goal = s.Goal,
                    dob = s.Dob,
                    gender = s.Gender,
                    caste = s.Caste,
                    address = s.Address,
                    profile_photo = s.ProfilePhoto,
                    profile_image = s.ProfilePhoto,
                    parent_mobile = s.ParentMobile,
                    status = s.Status,
                    suspension_reason = s.SuspensionReason,
                    suspended_at = s.SuspendedAt,
                    preferred_language = s.PreferredLanguage,
                    created_at = s.CreatedAt,
                    updated_at = s.UpdatedAt,
                    joining_date = s.JoiningDate
                })
                .ToListAsync(ct);

            var results = students.Select(s => {
                var photoPath = !string.IsNullOrEmpty(s.profile_photo) ? (s.profile_photo.StartsWith("http") ? s.profile_photo : $"/media/{s.profile_photo}") : null;
                return new {
                    s.id, s.user_id, s.student_id, s.username, s.first_name, s.middle_name, s.last_name, s.email, s.mobile,
                    s.is_active, s.goal, s.dob, s.gender, s.caste, s.address, 
                    profile_photo = photoPath,
                    profile_image = photoPath,
                    s.parent_mobile, s.status, s.suspension_reason, s.suspended_at, s.preferred_language, s.created_at, s.updated_at, s.joining_date
                };
            });

            return ServiceResult<object>.Ok(results);
        }

        public async Task<ServiceResult<object>> AssignMembershipAsync(AdminBillingController.MembershipAssignPayload payload, CancellationToken ct = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync(ct);
                try
            {
                var student = await _context.AccountsCustomusers.FindAsync(new object[] { payload.student_id }, ct);
                if (student == null) return ServiceResult<object>.NotFound("Student not found");

                var plan = await _context.MembershipsMembershipplans.FindAsync(new object[] { payload.plan_id }, ct);
                if (plan == null) return ServiceResult<object>.NotFound("Plan not found");

                DateOnly startDate = DateOnly.FromDateTime(DateTime.UtcNow);
                if (!string.IsNullOrEmpty(payload.start_date) && DateOnly.TryParse(payload.start_date, out var parsedStart))
                {
                    startDate = parsedStart;
                }

                DateOnly endDate = startDate.AddDays(plan.DurationDays > 0 ? plan.DurationDays : 30);
                if (!string.IsNullOrEmpty(payload.end_date) && DateOnly.TryParse(payload.end_date, out var parsedEnd))
                {
                    endDate = parsedEnd;
                }

                var membership = new MembershipsMembership
                {
                    StudentId = payload.student_id,
                    PlanId = payload.plan_id,
                    PlanNameSnapshot = plan.Name,
                    PriceSnapshot = plan.Price,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                };

                _context.MembershipsMemberships.Add(membership);
                student.IsActive = true;
                
                var profile = await _context.StudentsStudentprofiles.FirstOrDefaultAsync(p => p.UserId == payload.student_id, ct);
                if (profile != null)
                {
                    profile.Status = "LIVE";
                }

                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                if (!string.IsNullOrWhiteSpace(student.Email) || !string.IsNullOrWhiteSpace(student.Mobile))
                {
                    var email = student.Email;
                    var planName = plan.Name;
                    var validUntil = endDate.ToString("dd MMM yyyy");
                    var fName = student.FirstName ?? "Student";
                    var studentId = student.Id;
                    
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = _scopeFactory.CreateScope();
                            if (!string.IsNullOrWhiteSpace(email))
                            {
                                var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                                await emailSvc.SendPlanDetailsEmailAsync(email, planName, validUntil, "Unassigned");
                            }
                            
                            var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
                            string whatsappMsg = $"📚 *Plan Activated*\n\nHi {fName},\nYour {planName} plan has been successfully activated. It is valid until {validUntil}. Happy studying!";
                            await dispatcher.SendToStudentAsync(studentId, "Plan Activated ✅", $"Your {planName} plan is active until {validUntil}.", WebApplication1.Utils.NotificationTypes.Billing, whatsappMessage: whatsappMsg);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error sending plan details email/notification: {ex}");
                        }
                    });
                }

                return ServiceResult<object>.Ok(membership);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
            });
        }

        public async Task<ServiceResult<object>> GetMembershipsListAsync(int page, int pageSize, string search, string status, long? studentId, string nextTemplate, string prevTemplate, CancellationToken ct = default)
        {
            var query = _context.MembershipsMemberships
                .AsNoTracking()
                .Include(m => m.Student)
                .Include(m => m.Plan)
                .AsQueryable();

            if (studentId.HasValue)
                query = query.Where(m => m.StudentId == studentId.Value);

            if (!string.IsNullOrEmpty(status))
            {
                var lowerStatus = status.ToLower();
                query = query.Where(m => m.Status.ToLower() == lowerStatus);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(m => 
                    (m.Student.FirstName != null && m.Student.FirstName.ToLower().Contains(lowerSearch)) || 
                    (m.Student.LastName != null && m.Student.LastName.ToLower().Contains(lowerSearch)) || 
                    (m.Student.Username != null && m.Student.Username.ToLower().Contains(lowerSearch)) ||
                    (m.Plan.Name != null && m.Plan.Name.ToLower().Contains(lowerSearch)));
            }

            var totalCount = await query.CountAsync(ct);
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);

            var memberships = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new {
                    id = m.Id,
                    student = m.StudentId,
                    student_name = m.Student != null ? (m.Student.FirstName + " " + m.Student.LastName).Trim() : "",
                    plan = m.PlanId,
                    plan_name = m.Plan != null ? m.Plan.Name : m.PlanNameSnapshot,
                    plan_name_snapshot = m.PlanNameSnapshot,
                    start_date = m.StartDate.ToString("yyyy-MM-dd"),
                    end_date = m.EndDate.ToString("yyyy-MM-dd"),
                    status = m.Status,
                    price_snapshot = m.PriceSnapshot.ToString(),
                    is_active = m.IsActive,
                    notes = m.Notes,
                    created_at = m.CreatedAt,
                    renewal_count = 0
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(new {
                count = totalCount,
                total_pages = totalPages,
                current_page = page,
                next = page < totalPages ? nextTemplate.Replace("{P}", (page + 1).ToString()) : null,
                previous = page > 1 ? prevTemplate.Replace("{P}", (page - 1).ToString()) : null,
                data = memberships
            });
        }

        public async Task<ServiceResult<object>> GetPaymentsSummaryAsync(CancellationToken ct = default)
        {
            TimeSpan istOffset = TimeSpan.FromMinutes(330);
            DateTime nowUtc = DateTime.UtcNow;
            DateTime nowIst = nowUtc.Add(istOffset);
            
            DateOnly todayIstDate = DateOnly.FromDateTime(nowIst);
            DateOnly monthIstDate = new DateOnly(nowIst.Year, nowIst.Month, 1);
            DateOnly yearIstDate = new DateOnly(nowIst.Year, 1, 1);

            DateTime todayStartUtc = DateTime.SpecifyKind(nowIst.Date.Subtract(istOffset), DateTimeKind.Utc);
            DateTime todayEndUtc = DateTime.SpecifyKind(todayStartUtc.AddDays(1), DateTimeKind.Utc);
            
            DateTime monthStartUtc = DateTime.SpecifyKind(new DateTime(nowIst.Year, nowIst.Month, 1).Subtract(istOffset), DateTimeKind.Utc);
            DateTime yearStartUtc = DateTime.SpecifyKind(new DateTime(nowIst.Year, 1, 1).Subtract(istOffset), DateTimeKind.Utc);

            var pendingCount = await _context.PaymentsPayments.CountAsync(p => p.Status.ToLower() == "pending", ct);

            var stats = await _context.PaymentsPayments
                .AsNoTracking()
                .GroupBy(p => 1)
                .Select(g => new {
                    todayCount = g.Count(p => p.PaymentDate == todayIstDate),
                    todayReceived = g.Sum(p => p.PaymentDate == todayIstDate && (p.Status.ToLower() == "verified" || p.Status.ToLower() == "refunded") ? (decimal?)p.Amount : 0) ?? 0,
                    todayRefunds = g.Sum(p => p.Status.ToLower() == "refunded" && p.RefundedAt != null && p.RefundedAt >= todayStartUtc && p.RefundedAt < todayEndUtc ? (decimal?)(p.RefundAmount ?? p.Amount) : 0) ?? 0,
                    
                    monthReceived = g.Sum(p => p.PaymentDate >= monthIstDate && (p.Status.ToLower() == "verified" || p.Status.ToLower() == "refunded") ? (decimal?)p.Amount : 0) ?? 0,
                    monthRefunds = g.Sum(p => p.Status.ToLower() == "refunded" && p.RefundedAt != null && p.RefundedAt >= monthStartUtc ? (decimal?)(p.RefundAmount ?? p.Amount) : 0) ?? 0,
                    
                    yearReceived = g.Sum(p => p.PaymentDate >= yearIstDate && (p.Status.ToLower() == "verified" || p.Status.ToLower() == "refunded") ? (decimal?)p.Amount : 0) ?? 0,
                    yearRefunds = g.Sum(p => p.Status.ToLower() == "refunded" && p.RefundedAt != null && p.RefundedAt >= yearStartUtc ? (decimal?)(p.RefundAmount ?? p.Amount) : 0) ?? 0,

                    allTimeReceived = g.Sum(p => (p.Status.ToLower() == "verified" || p.Status.ToLower() == "refunded") ? (decimal?)p.Amount : 0) ?? 0,
                    allTimeRefunds = g.Sum(p => p.Status.ToLower() == "refunded" && p.RefundedAt != null ? (decimal?)(p.RefundAmount ?? p.Amount) : 0) ?? 0
                })
                .FirstOrDefaultAsync(ct);

            var todayAmount = (stats?.todayReceived ?? 0) - (stats?.todayRefunds ?? 0);
            var todayCount = stats?.todayCount ?? 0;
            var monthAmount = (stats?.monthReceived ?? 0) - (stats?.monthRefunds ?? 0);
            var yearAmount = (stats?.yearReceived ?? 0) - (stats?.yearRefunds ?? 0);
            var allTimeAmount = (stats?.allTimeReceived ?? 0) - (stats?.allTimeRefunds ?? 0);

            return ServiceResult<object>.Ok(new {
                today_amount = todayAmount.ToString("0.00"),
                today_count = todayCount,
                month_amount = monthAmount.ToString("0.00"),
                year_amount = yearAmount.ToString("0.00"),
                all_time_amount = allTimeAmount.ToString("0.00"),
                pending_count = pendingCount
            });
        }

        public async Task<ServiceResult<object>> GetPendingPaymentsAsync(CancellationToken ct = default)
        {
            var payments = await _context.PaymentsPayments
                .AsNoTracking()
                .Include(p => p.Student)
                    .ThenInclude(s => s!.StudentsStudentprofile)
                .Include(p => p.Membership)
                    .ThenInclude(m => m!.Plan)
                .Where(p => p.Status.ToLower() == "pending")
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new {
                    id = p.Id,
                    payment_id = p.PaymentId,
                    student = p.StudentId,
                    student_name = p.Student != null ? (p.Student.FirstName + " " + p.Student.LastName).Trim() : "",
                    student_profile_photo = p.Student != null && p.Student.StudentsStudentprofile != null ? p.Student.StudentsStudentprofile.ProfilePhoto : null,
                    membership = p.MembershipId,
                    plan_name = p.Membership != null && p.Membership.Plan != null ? p.Membership.Plan.Name : null,
                    plan_start = p.Membership != null ? p.Membership.StartDate.ToString("yyyy-MM-dd") : null,
                    plan_end = p.Membership != null ? p.Membership.EndDate.ToString("yyyy-MM-dd") : null,
                    amount = p.Amount.ToString(),
                    status = p.Status,
                    method = p.Method,
                    payment_mode = p.PaymentMode,
                    payment_date = p.PaymentDate.ToString("yyyy-MM-dd"),
                    paid_at = p.PaidAt,
                    verified_at = p.VerifiedAt,
                    transaction_ref = p.TransactionRef,
                    transaction_id = p.TransactionId,
                    receipt_url = p.ReceiptUrl,
                    refund_amount = p.RefundAmount != null ? p.RefundAmount.ToString() : null,
                    refund_reason = p.RefundReason,
                    refunded_at = p.RefundedAt,
                    notes = p.Notes
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(payments);
        }

        public async Task<ServiceResult<object>> GetOverduePaymentsAsync(CancellationToken ct = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var payments = await _context.PaymentsPayments
                .AsNoTracking()
                .Include(p => p.Student)
                    .ThenInclude(s => s!.StudentsStudentprofile)
                .Include(p => p.Membership)
                    .ThenInclude(m => m!.Plan)
                .Where(p => p.Status.ToLower() == "pending" && p.PaymentDate < today)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new {
                    id = p.Id,
                    payment_id = p.PaymentId,
                    student = p.StudentId,
                    student_name = p.Student != null ? (p.Student.FirstName + " " + p.Student.LastName).Trim() : "",
                    student_profile_photo = p.Student != null && p.Student.StudentsStudentprofile != null ? p.Student.StudentsStudentprofile.ProfilePhoto : null,
                    membership = p.MembershipId,
                    plan_name = p.Membership != null && p.Membership.Plan != null ? p.Membership.Plan.Name : null,
                    plan_start = p.Membership != null ? p.Membership.StartDate.ToString("yyyy-MM-dd") : null,
                    plan_end = p.Membership != null ? p.Membership.EndDate.ToString("yyyy-MM-dd") : null,
                    amount = p.Amount.ToString(),
                    status = p.Status,
                    method = p.Method,
                    payment_mode = p.PaymentMode,
                    payment_date = p.PaymentDate.ToString("yyyy-MM-dd"),
                    paid_at = p.PaidAt,
                    verified_at = p.VerifiedAt,
                    transaction_ref = p.TransactionRef,
                    transaction_id = p.TransactionId,
                    receipt_url = p.ReceiptUrl,
                    refund_amount = p.RefundAmount != null ? p.RefundAmount.ToString() : null,
                    refund_reason = p.RefundReason,
                    refunded_at = p.RefundedAt,
                    notes = p.Notes
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(payments);
        }

        public async Task<ServiceResult<object>> GetPaymentsListAsync(int page, int pageSize, string search, string status, string nextTemplate, string prevTemplate, CancellationToken ct = default)
        {
            var query = _context.PaymentsPayments
                .AsNoTracking()
                .Include(p => p.Student)
                    .ThenInclude(s => s!.StudentsStudentprofile)
                .Include(p => p.Membership)
                    .ThenInclude(m => m!.Plan)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                var lowerStatus = status.ToLower();
                query = query.Where(p => p.Status.ToLower() == lowerStatus);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(p => 
                    (p.Student.FirstName != null && p.Student.FirstName.ToLower().Contains(lowerSearch)) || 
                    (p.Student.LastName != null && p.Student.LastName.ToLower().Contains(lowerSearch)) || 
                    (p.Student.Username != null && p.Student.Username.ToLower().Contains(lowerSearch)));
            }

            var totalCount = await query.CountAsync(ct);
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);

            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new {
                    id = p.Id,
                    payment_id = p.PaymentId,
                    student = p.StudentId,
                    student_name = p.Student != null ? (p.Student.FirstName + " " + p.Student.LastName).Trim() : "",
                    student_profile_photo = p.Student != null && p.Student.StudentsStudentprofile != null ? p.Student.StudentsStudentprofile.ProfilePhoto : null,
                    membership = p.MembershipId,
                    plan_name = p.Membership != null && p.Membership.Plan != null ? p.Membership.Plan.Name : null,
                    plan_start = p.Membership != null ? p.Membership.StartDate.ToString("yyyy-MM-dd") : null,
                    plan_end = p.Membership != null ? p.Membership.EndDate.ToString("yyyy-MM-dd") : null,
                    amount = p.Amount.ToString(),
                    status = p.Status,
                    method = p.Method,
                    payment_mode = p.PaymentMode,
                    payment_date = p.PaymentDate.ToString("yyyy-MM-dd"),
                    paid_at = p.PaidAt,
                    verified_at = p.VerifiedAt,
                    transaction_ref = p.TransactionRef,
                    transaction_id = p.TransactionId,
                    receipt_url = p.ReceiptUrl,
                    refund_amount = p.RefundAmount != null ? p.RefundAmount.ToString() : null,
                    refund_reason = p.RefundReason,
                    refunded_at = p.RefundedAt,
                    notes = p.Notes
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(new {
                count = totalCount,
                total_pages = totalPages,
                current_page = page,
                next = page < totalPages ? nextTemplate.Replace("{P}", (page + 1).ToString()) : null,
                previous = page > 1 ? prevTemplate.Replace("{P}", (page - 1).ToString()) : null,
                data = payments
            });
        }

        public async Task<ServiceResult<object>> CreatePaymentAsync(AdminBillingController.PaymentCreatePayload payload, CancellationToken ct = default)
        {
            var student = await _context.AccountsCustomusers.FindAsync(new object[] { (long)payload.student_id }, ct);
            if (student == null) return ServiceResult<object>.NotFound("Student not found");

            decimal amount = 0;
            MembershipsMembershipplan? plan = null;
            if (payload.plan_id.HasValue && payload.plan_id > 0)
            {
                plan = await _context.MembershipsMembershipplans.FindAsync(new object[] { (long)payload.plan_id.Value }, ct);
                if (plan != null)
                {
                    var baseDuration = plan.DurationDays > 0 ? plan.DurationDays : 30;
                    var pricePerDay = plan.Price / baseDuration;
                    amount = pricePerDay * payload.duration_days;
                }
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(ct);
                try
            {
                long? membershipId = null;
                if (plan != null)
                {
                    var membership = new MembershipsMembership
                    {
                        StudentId = payload.student_id,
                        PlanId = plan.Id,
                        PlanNameSnapshot = plan.Name,
                        PriceSnapshot = plan.Price,
                        StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(payload.duration_days)),
                        Status = "active",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.MembershipsMemberships.Add(membership);
                    
                    var profile = await _context.StudentsStudentprofiles.FirstOrDefaultAsync(p => p.UserId == payload.student_id, ct);
                    if (profile != null)
                    {
                        profile.Status = "LIVE";
                    }
                    
                    await _context.SaveChangesAsync(ct);
                    membershipId = membership.Id;
                }

                var payment = new PaymentsPayment
                {
                    PaymentId = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}",
                    StudentId = payload.student_id,
                    MembershipId = membershipId,
                    Amount = amount,
                    Status = "verified",
                    Method = "CASH",
                    PaymentMode = payload.payment_mode ?? "Cash",
                    PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    PaidAt = DateTime.UtcNow,
                    VerifiedAt = DateTime.UtcNow,
                    TransactionRef = payload.transaction_ref,
                    Notes = payload.notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PaymentsPayments.Add(payment);
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _ = Task.Run(async () =>
                {
                    try 
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var billingSvc = scope.ServiceProvider.GetRequiredService<IAdminBillingService>();
                        await billingSvc.SendPaymentReceiptEmailAsync(payment.Id);
                        
                        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
                        byte[]? pdfBytes = null;
                        var pdfResult = await billingSvc.GetPaymentReceiptPdfAsync(payment.Id);
                        if (pdfResult.Success && pdfResult.Data is byte[] b)
                        {
                            pdfBytes = b;
                        }
                        
                        string msg = $"🧾 *Payment Confirmed*\n\nYour payment of ₹{payment.Amount} has been recorded successfully. Please find your receipt attached.";
                        await dispatcher.SendToStudentAsync(payload.student_id, "Payment Confirmed ✅", $"Payment of ₹{payment.Amount} recorded.", WebApplication1.Utils.NotificationTypes.Billing, whatsappMessage: msg, pdfBytes: pdfBytes, pdfFileName: $"Receipt_{payment.PaymentId}.pdf");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending receipt email/notification: {ex}");
                    }
                    
                });

                return ServiceResult<object>.Ok(new { id = payment.Id });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
            });
        }

        public async Task<ServiceResult<object>> GetPaymentDetailAsync(long id, CancellationToken ct = default)
        {
            var payment = await _context.PaymentsPayments
                .AsNoTracking()
                .Include(p => p.Student)
                .Include(p => p.Membership)
                    .ThenInclude(m => m!.Plan)
                .Where(p => p.Id == id)
                .Select(p => new {
                    id = p.Id,
                    payment_id = p.PaymentId,
                    student = p.StudentId,
                    student_name = p.Student != null ? (p.Student.FirstName + " " + p.Student.LastName).Trim() : "",
                    membership = p.MembershipId,
                    plan_name = p.Membership != null && p.Membership.Plan != null ? p.Membership.Plan.Name : null,
                    amount = p.Amount.ToString(),
                    status = p.Status,
                    method = p.Method,
                    payment_mode = p.PaymentMode,
                    payment_date = p.PaymentDate.ToString("yyyy-MM-dd"),
                    paid_at = p.PaidAt,
                    verified_at = p.VerifiedAt,
                    transaction_ref = p.TransactionRef,
                    transaction_id = p.TransactionId,
                    receipt_url = p.ReceiptUrl,
                    refund_amount = p.RefundAmount != null ? p.RefundAmount.ToString() : null,
                    refund_reason = p.RefundReason,
                    refunded_at = p.RefundedAt,
                    notes = p.Notes
                })
                .FirstOrDefaultAsync(ct);

            if (payment == null) return ServiceResult<object>.NotFound("Payment not found");
            return ServiceResult<object>.Ok(payment);
        }

        public async Task<ServiceResult<object>> VerifyPaymentAsync(long id, CancellationToken ct = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(ct);
                try
            {
                var payment = await _context.PaymentsPayments.FindAsync(new object[] { id }, ct);
                if (payment == null) return ServiceResult<object>.NotFound("Payment not found");

                payment.Status = "verified";
                payment.VerifiedAt = DateTime.UtcNow;
                
                if (payment.MembershipId.HasValue)
                {
                    var membership = await _context.MembershipsMemberships.FindAsync(new object[] { payment.MembershipId.Value }, ct);
                    if (membership != null)
                    {
                        if (membership.Status != "active")
                        {
                            membership.Status = "active";
                            var student = await _context.AccountsCustomusers.FindAsync(new object[] { membership.StudentId }, ct);
                            if (student != null)
                            {
                                student.IsActive = true;
                            }
                        }

                        var profile = await _context.StudentsStudentprofiles.FirstOrDefaultAsync(p => p.UserId == membership.StudentId, ct);
                        if (profile != null)
                        {
                            profile.Status = "LIVE";
                        }
                    }
                }
                
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _ = Task.Run(async () =>
                {
                    try 
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var billingSvc = scope.ServiceProvider.GetRequiredService<IAdminBillingService>();
                        await billingSvc.SendPaymentReceiptEmailAsync(id);
                        
                        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
                        byte[]? pdfBytes = null;
                        var pdfResult = await billingSvc.GetPaymentReceiptPdfAsync(payment.Id);
                        if (pdfResult.Success && pdfResult.Data is byte[] b)
                        {
                            pdfBytes = b;
                        }
                        
                        var studentId = payment.StudentId;
                        if (studentId > 0)
                        {
                            string msg = $"🧾 *Payment Verified*\n\nYour payment of ₹{payment.Amount} has been verified successfully. Please find your receipt attached.";
                            await dispatcher.SendToStudentAsync(studentId, "Payment Verified ✅", $"Payment of ₹{payment.Amount} verified.", WebApplication1.Utils.NotificationTypes.Billing, whatsappMessage: msg, pdfBytes: pdfBytes, pdfFileName: $"Receipt_{payment.PaymentId}.pdf");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending receipt email/notification: {ex}");
                    }

                });

                return ServiceResult<object>.Ok(new {
                    id = payment.Id,
                    status = payment.Status,
                    verified_at = payment.VerifiedAt
                });
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
            });
        }

        public async Task<ServiceResult<object>> RefundPaymentAsync(long id, AdminBillingController.RefundPayload payload, CancellationToken ct = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(ct);
                try
            {
                var payment = await _context.PaymentsPayments.FindAsync(new object[] { id }, ct);
                if (payment == null) return ServiceResult<object>.NotFound("Payment not found");

                if (payment.MembershipId.HasValue)
                {
                    var membershipCheck = await _context.MembershipsMemberships.FindAsync(new object[] { payment.MembershipId.Value }, ct);
                    if (membershipCheck != null)
                    {
                        var today = DateOnly.FromDateTime(DateTime.UtcNow);
                        if (membershipCheck.EndDate < today || membershipCheck.Status.ToLower() == "expired" || membershipCheck.Status.ToLower() == "completed")
                        {
                            return ServiceResult<object>.Fail("Cannot refund payment for an expired or completed plan.");
                        }
                    }
                }

                payment.Status = "refunded";
                payment.RefundAmount = payload.refund_amount ?? payment.Amount;
                payment.RefundReason = payload.refund_reason;
                payment.RefundedAt = DateTime.UtcNow;

                if (payment.RefundAmount >= payment.Amount && payment.MembershipId.HasValue)
                {
                    var membership = await _context.MembershipsMemberships.FindAsync(new object[] { payment.MembershipId.Value }, ct);
                    if (membership != null)
                    {
                        membership.Status = "cancelled";
                        membership.IsActive = false;
                    }
                }

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _ = Task.Run(async () =>
                {
                    try 
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var billingSvc = scope.ServiceProvider.GetRequiredService<IAdminBillingService>();
                        await billingSvc.SendPaymentRefundEmailAsync(payment.Id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending refund email: {ex}");
                    }
                });

                return ServiceResult<object>.Ok(new {
                    id = payment.Id,
                    status = payment.Status,
                    refund_amount = payment.RefundAmount,
                    refund_reason = payment.RefundReason,
                    refunded_at = payment.RefundedAt
                });
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
            });
        }

        public async Task<ServiceResult<object>> UpdatePaymentAsync(long id, AdminBillingController.PaymentUpdateDto payload, CancellationToken ct = default)
        {
            var payment = await _context.PaymentsPayments.FindAsync(new object[] { id }, ct);
            if (payment == null) return ServiceResult<object>.NotFound("Payment not found");

            if (payload.status != null)
                payment.Status = payload.status.ToLower();
            
            if (payload.payment_mode != null)
                payment.PaymentMode = payload.payment_mode;

            if (payload.transaction_ref != null)
                payment.TransactionRef = payload.transaction_ref;

            if (payload.notes != null)
                payment.Notes = payload.notes;

            await _context.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok(new {
                id = payment.Id,
                status = payment.Status,
                payment_mode = payment.PaymentMode,
                transaction_ref = payment.TransactionRef,
                notes = payment.Notes
            });
        }

        public async Task<ServiceResult<object>> GetMembershipDetailAsync(long id, CancellationToken ct = default)
        {
            var membership = await _context.MembershipsMemberships
                .AsNoTracking()
                .Include(m => m.Student)
                .Include(m => m.Plan)
                .Where(m => m.Id == id)
                .Select(m => new {
                    id = m.Id,
                    student = m.StudentId,
                    student_name = m.Student != null ? (m.Student.FirstName + " " + m.Student.LastName).Trim() : "",
                    plan = m.PlanId,
                    plan_name = m.Plan != null ? m.Plan.Name : m.PlanNameSnapshot,
                    start_date = m.StartDate.ToString("yyyy-MM-dd"),
                    end_date = m.EndDate.ToString("yyyy-MM-dd"),
                    status = m.Status,
                    price_snapshot = m.PriceSnapshot.ToString(),
                    is_active = m.IsActive,
                    notes = m.Notes,
                    created_at = m.CreatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (membership == null) return ServiceResult<object>.NotFound("Membership not found");
            return ServiceResult<object>.Ok(membership);
        }

        public async Task<ServiceResult<object>> GetExpiringMembershipsAsync(int days, CancellationToken ct = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var threshold = today.AddDays(days);
            
            var memberships = await _context.MembershipsMemberships
                .AsNoTracking()
                .Include(m => m.Student)
                .Include(m => m.Plan)
                .Where(m => m.Status.ToLower() == "active" && m.EndDate >= today && m.EndDate <= threshold)
                .Select(m => new {
                    id = m.Id,
                    student = m.StudentId,
                    student_name = m.Student != null ? (m.Student.FirstName + " " + m.Student.LastName).Trim() : "",
                    plan = m.PlanId,
                    plan_name = m.Plan != null ? m.Plan.Name : m.PlanNameSnapshot,
                    end_date = m.EndDate.ToString("yyyy-MM-dd"),
                    status = m.Status
                })
                .ToListAsync(ct);
                
            return ServiceResult<object>.Ok(memberships);
        }

        public async Task<ServiceResult<object>> GetExpiredTodayMembershipsAsync(CancellationToken ct = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            
            var memberships = await _context.MembershipsMemberships
                .AsNoTracking()
                .Include(m => m.Student)
                .Include(m => m.Plan)
                .Where(m => m.Status.ToLower() == "active" && m.EndDate < today)
                .Select(m => new {
                    id = m.Id,
                    student = m.StudentId,
                    student_name = m.Student != null ? (m.Student.FirstName + " " + m.Student.LastName).Trim() : "",
                    plan = m.PlanId,
                    plan_name = m.Plan != null ? m.Plan.Name : m.PlanNameSnapshot,
                    end_date = m.EndDate.ToString("yyyy-MM-dd"),
                    status = m.Status
                })
                .ToListAsync(ct);
                
            return ServiceResult<object>.Ok(memberships);
        }

        public async Task<ServiceResult<object>> RenewMembershipAsync(AdminBillingController.MembershipAssignPayload payload, CancellationToken ct = default)
        {
            return await AssignMembershipAsync(payload, ct);
        }

        public async Task<ServiceResult<object>> UpgradeMembershipAsync(AdminBillingController.MembershipAssignPayload payload, CancellationToken ct = default)
        {
            return await AssignMembershipAsync(payload, ct);
        }

        public async Task<ServiceResult<object>> GetPaymentReceiptPdfAsync(long id, CancellationToken ct = default)
        {
            var payment = await _context.PaymentsPayments
                .AsNoTracking()
                .Include(p => p.Student)
                .Include(p => p.Membership)
                    .ThenInclude(m => m!.Plan)
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync(ct);

            if (payment == null) return ServiceResult<object>.NotFound("Payment not found");

            byte[] pdfBytes = await GenerateReceiptPdfAsync(payment, ct);
            return ServiceResult<object>.Ok(pdfBytes);
        }

        public async Task<ServiceResult<object>> SendPaymentReceiptEmailAsync(long id, CancellationToken ct = default)
        {
            var payment = await _context.PaymentsPayments
                .AsNoTracking()
                .Include(p => p.Student)
                    .ThenInclude(s => s!.StudentsStudentprofile)
                .Include(p => p.Membership)
                    .ThenInclude(m => m!.Plan)
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync(ct);

            if (payment == null) return ServiceResult<object>.NotFound("Payment not found");
            if (payment.Student == null || string.IsNullOrEmpty(payment.Student.Email))
                return ServiceResult<object>.Fail("Student email not found.");

            byte[] pdfBytes = await GenerateReceiptPdfAsync(payment, ct);
            
            var subject = $"Plan Activated & Payment Receipt - {payment.PaymentId ?? $"TXN{payment.Id}"}";
            var studentName = string.IsNullOrWhiteSpace(payment.Student.FirstName) ? "Student" : payment.Student.FirstName;
            var goal = payment.Student.StudentsStudentprofile?.Goal ?? "Excellence";
            
            var stats = new System.Collections.Generic.Dictionary<string, string> {
                { "Goal", goal },
                { "Plan Details", payment.Membership?.Plan?.Name ?? payment.Membership?.PlanNameSnapshot ?? "Standalone Payment" },
                { "Starts", payment.Membership != null ? payment.Membership.StartDate.ToString("dd MMM yyyy") : "N/A" },
                { "Expires", payment.Membership != null ? payment.Membership.EndDate.ToString("dd MMM yyyy") : "N/A" },
                { "Amount Paid", $"₹{payment.Amount:0.00}" },
                { "Payment Mode", payment.PaymentMode ?? "N/A" }
            };

            var htmlMessage = EmailTemplateBuilder.BuildTemplate(
                title: $"Congratulations {studentName}!",
                subtitle: "Your plan has been activated successfully. Your payment receipt is attached as a PDF.",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/congratulations.png",
                colorStart: "#6366f1", // indigo-500
                colorEnd: "#9333ea",   // purple-600
                highlight: null,
                actionText: "View Dashboard",
                footer: "Thank you for choosing Shresht Library!",
                stats: stats
            );

            await _emailService.SendEmailWithAttachmentAsync(
                payment.Student.Email,
                subject,
                htmlMessage,
                pdfBytes,
                $"Receipt_{payment.PaymentId ?? $"TXN{payment.Id}"}.pdf"
            );

            if (!string.IsNullOrWhiteSpace(payment.Student.Mobile))
            {
                var planName = payment.Membership?.Plan?.Name ?? payment.Membership?.PlanNameSnapshot ?? "Standalone Payment";
                var msg = $"✅ *Payment Successful*\n\nHi {studentName},\nWe've received your payment of ₹{payment.Amount:0.00} for {planName}.\nYour receipt has been sent to your email. Thanks for choosing Shresht Library!";
                var fileName = $"Receipt_{payment.PaymentId ?? $"TXN{payment.Id}"}.pdf";
                await _whatsappService.SendDocumentAsync(payment.Student.Mobile, pdfBytes, fileName, msg);
            }

            return ServiceResult<object>.Ok(new { success = true });
        }

        public async Task<ServiceResult<object>> SendPaymentRefundEmailAsync(long id, CancellationToken ct = default)
        {
            var payment = await _context.PaymentsPayments
                .AsNoTracking()
                .Include(p => p.Student)
                    .ThenInclude(s => s!.StudentsStudentprofile)
                .Include(p => p.Membership)
                    .ThenInclude(m => m!.Plan)
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync(ct);

            if (payment == null) return ServiceResult<object>.NotFound("Payment not found");
            if (payment.Student == null || string.IsNullOrEmpty(payment.Student.Email))
                return ServiceResult<object>.Fail("Student email not found.");

            byte[] pdfBytes = await GenerateReceiptPdfAsync(payment, ct);
            
            var subject = $"Payment Refunded - {payment.PaymentId ?? $"TXN{payment.Id}"}";
            var studentName = string.IsNullOrWhiteSpace(payment.Student.FirstName) ? "Student" : payment.Student.FirstName;
            var goal = payment.Student.StudentsStudentprofile?.Goal ?? "Excellence";
            
            var stats = new System.Collections.Generic.Dictionary<string, string> {
                { "Goal", goal },
                { "Plan Details", payment.Membership?.Plan?.Name ?? payment.Membership?.PlanNameSnapshot ?? "Standalone Payment" },
                { "Original Amount", $"₹{payment.Amount:0.00}" },
                { "Refunded Amount", $"₹{(payment.RefundAmount ?? payment.Amount):0.00}" },
                { "Refund Date", payment.RefundedAt?.ToString("dd MMM yyyy") ?? DateTime.UtcNow.ToString("dd MMM yyyy") },
                { "Reason", string.IsNullOrEmpty(payment.RefundReason) ? "Admin Refund" : payment.RefundReason }
            };

            var htmlMessage = EmailTemplateBuilder.BuildTemplate(
                title: $"Payment Refunded",
                subtitle: $"Dear {studentName}, your payment has been successfully refunded. Your refund receipt is attached as a PDF.",
                imageUrl: "https://raw.githubusercontent.com/tarurinfotech/shreshtibrary/main/public/images/emails/refund.png",
                colorStart: "#eab308", // yellow-500
                colorEnd: "#d97706",   // amber-600
                highlight: null,
                actionText: "View Dashboard",
                footer: "If you have any questions, please contact us.",
                stats: stats
            );

            await _emailService.SendEmailWithAttachmentAsync(
                payment.Student.Email,
                subject,
                htmlMessage,
                pdfBytes,
                $"Refund_Receipt_{payment.PaymentId ?? $"TXN{payment.Id}"}.pdf"
            );

            if (!string.IsNullOrWhiteSpace(payment.Student.Mobile))
            {
                var planName = payment.Membership?.Plan?.Name ?? payment.Membership?.PlanNameSnapshot ?? "Standalone Payment";
                var msg = $"🔄 *Payment Refunded*\n\nHi {studentName},\nYour payment of ₹{payment.Amount:0.00} for {planName} has been refunded.\nAmount Refunded: ₹{(payment.RefundAmount ?? payment.Amount):0.00}.\nYour refund receipt has been sent to your email.";
                var fileName = $"Refund_Receipt_{payment.PaymentId ?? $"TXN{payment.Id}"}.pdf";
                await _whatsappService.SendDocumentAsync(payment.Student.Mobile, pdfBytes, fileName, msg);
            }

            return ServiceResult<object>.Ok(new { success = true });
        }

        private async Task<byte[]> GenerateReceiptPdfAsync(PaymentsPayment payment, CancellationToken ct)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var libraryInfo = await _context.LibraryLibraryinfos.FirstOrDefaultAsync(ct);
            byte[]? logoBytes = null;

            if (libraryInfo != null)
            {
                var imageUrl = !string.IsNullOrWhiteSpace(libraryInfo.BannerImage) ? libraryInfo.BannerImage : libraryInfo.Logo;
                if (!string.IsNullOrWhiteSpace(imageUrl) && imageUrl.StartsWith("http"))
                {
                    try
                    {
                        using var client = new System.Net.Http.HttpClient();
                        logoBytes = await client.GetByteArrayAsync(imageUrl, ct);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching logo for receipt: {ex}");
                    }
                }
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Content().Column(col =>
                    {
                        // Add Logo on top center
                        if (logoBytes != null)
                        {
                            col.Item().AlignCenter().Height(50).Image(logoBytes).FitArea();
                            col.Item().PaddingBottom(10);
                        }
                        else if (libraryInfo != null && !string.IsNullOrEmpty(libraryInfo.LibraryName))
                        {
                            col.Item().AlignCenter().Text(libraryInfo.LibraryName).FontSize(16).Bold();
                            col.Item().PaddingBottom(10);
                        }

                        // Dynamic Status Icon and Text
                        var isRefunded = payment.Status.ToLower() == "refunded";
                        var isPending = payment.Status.ToLower() == "pending";

                        var statusColorHex = isRefunded ? "#ef4444" : (isPending ? "#f59e0b" : "#10b981");
                        var statusText = isRefunded ? "Refunded" : (isPending ? "Pending" : "Successful");
                        
                        var svgIcon = isRefunded 
                            ? $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"{statusColorHex}\" stroke-width=\"2.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><polyline points=\"1 4 1 10 7 10\"/><path d=\"M3.51 15a9 9 0 1 0 2.13-9.36L1 10\"/></svg>"
                            : (isPending 
                                ? $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"{statusColorHex}\" stroke-width=\"2.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><circle cx=\"12\" cy=\"12\" r=\"10\"/><polyline points=\"12 6 12 12 16 14\"/></svg>"
                                : $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"{statusColorHex}\" stroke-width=\"2.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M22 11.08V12a10 10 0 1 1-5.93-9.14\"/><polyline points=\"22 4 12 14.01 9 11.01\"/></svg>");
                        
                        col.Item().AlignCenter().Width(40).Height(40).Svg(svgIcon);
                        
                        col.Item().PaddingTop(15).AlignCenter().Text(statusText).FontSize(18).Bold().FontColor(Colors.Grey.Darken4);
                        
                        var studentName = "Student";
                        if (payment.Student != null)
                        {
                            studentName = (payment.Student.FirstName + " " + payment.Student.LastName).Trim();
                            if (string.IsNullOrWhiteSpace(studentName)) studentName = "Student";
                        }
                        
                        col.Item().PaddingTop(5).PaddingBottom(25).AlignCenter()
                            .Text($"{studentName}, your receipt has been generated.").FontSize(11).FontColor(Colors.Grey.Medium);
                        
                        // Details
                        col.Item().PaddingBottom(12).Row(row =>
                        {
                            row.RelativeItem().Text("Receipt ID").FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text(payment.PaymentId ?? payment.TransactionRef ?? $"TXN{payment.Id}").Bold();
                        });

                        col.Item().PaddingBottom(12).Row(row =>
                        {
                            row.RelativeItem().Text("Plan").FontColor(Colors.Grey.Medium);
                            var planName = payment.Membership?.Plan?.Name ?? payment.Membership?.PlanNameSnapshot ?? "Standalone Payment";
                            row.RelativeItem().AlignRight().Text(planName).Bold();
                        });

                        col.Item().PaddingBottom(20).Row(row =>
                        {
                            row.RelativeItem().Text("Date").FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text(payment.PaymentDate.ToString("MMM dd, yyyy")).Bold();
                        });

                        col.Item().PaddingBottom(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Breakdown
                        col.Item().PaddingBottom(12).Row(row =>
                        {
                            row.RelativeItem().Text("Plan fee").FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text($"Rs. {payment.Amount:0.00}").Bold();
                        });

                        col.Item().PaddingBottom(12).Row(row =>
                        {
                            row.RelativeItem().Text("Discount/Tax").FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text($"Rs. 0.00").Bold();
                        });

                        col.Item().PaddingBottom(20).Row(row =>
                        {
                            row.RelativeItem().Text(payment.Status.ToLower() == "refunded" ? "Total Refunded" : "Total charge").FontColor(Colors.Grey.Medium);
                            var amountDisplay = (payment.Status.ToLower() == "refunded" && payment.RefundAmount.HasValue) ? payment.RefundAmount.Value : payment.Amount;
                            row.RelativeItem().AlignRight().Text($"Rs. {amountDisplay:0.00}").FontSize(12).Bold().FontColor(Colors.Grey.Darken4);
                        });

                        col.Item().PaddingBottom(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Payment info
                        col.Item().PaddingBottom(12).Row(row =>
                        {
                            row.RelativeItem().Text("Payment method").FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text(payment.PaymentMode ?? "N/A").Bold();
                        });

                        col.Item().PaddingBottom(12).Row(row =>
                        {
                            row.RelativeItem().Text("Payment status").FontColor(Colors.Grey.Medium);
                            var statusColor = payment.Status.ToLower() == "verified" ? Colors.Green.Medium : 
                                              (payment.Status.ToLower() == "pending" ? Colors.Amber.Medium : Colors.Red.Medium);
                            row.RelativeItem().AlignRight().Text($"• {payment.Status.ToUpper()}").FontColor(statusColor).Bold();
                        });

                        // Library Information at the bottom
                        if (libraryInfo != null)
                        {
                            col.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            col.Item().PaddingTop(15).AlignCenter().Text("Library Information").FontSize(12).Bold().FontColor(Colors.Grey.Darken4);
                            
                            col.Item().PaddingTop(5).Column(libCol =>
                            {
                                if (!string.IsNullOrEmpty(libraryInfo.AddressLine1))
                                {
                                    var address = $"{libraryInfo.AddressLine1}";
                                    if (!string.IsNullOrEmpty(libraryInfo.AddressLine2)) address += $", {libraryInfo.AddressLine2}";
                                    address += $", {libraryInfo.City}, {libraryInfo.State} - {libraryInfo.PinCode}";
                                    libCol.Item().AlignCenter().Text(address).FontSize(10).FontColor(Colors.Grey.Medium);
                                }
                                
                                if (!string.IsNullOrEmpty(libraryInfo.ContactNumber))
                                {
                                    libCol.Item().AlignCenter().Text($"Phone: {libraryInfo.ContactNumber}").FontSize(10).FontColor(Colors.Grey.Medium);
                                }

                                if (!string.IsNullOrEmpty(libraryInfo.Website))
                                {
                                    libCol.Item().AlignCenter().Text($"Website: {libraryInfo.Website}").FontSize(10).FontColor(Colors.Grey.Medium);
                                }
                            });
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
