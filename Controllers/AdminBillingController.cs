using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.Services;
using WebApplication1.Models.Responses;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Roles = "admin,super_admin")]
    public class AdminBillingController : ControllerBase
    {
        private readonly IAdminBillingService _billingService;

        public AdminBillingController(IAdminBillingService billingService)
        {
            _billingService = billingService;
        }

        [HttpGet("plans/stats")]
        public async Task<IActionResult> PlanStatsAsync(CancellationToken ct)
        {
            var result = await _billingService.GetPlanStatsAsync(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("plans")]
        public async Task<IActionResult> PlansAllAsync(CancellationToken ct)
        {
            var result = await _billingService.GetAllPlansAsync(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        public class PlanPayload
        {
            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Name is required.")]
            [System.ComponentModel.DataAnnotations.MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
            public string name { get; set; } = string.Empty;

            [System.ComponentModel.DataAnnotations.Range(0, 120, ErrorMessage = "Duration in months must be between 0 and 120.")]
            public int duration_months { get; set; }

            [System.ComponentModel.DataAnnotations.Range(0, 3650, ErrorMessage = "Duration in days must be between 0 and 3650.")]
            public int? duration_days { get; set; }

            [System.ComponentModel.DataAnnotations.Range(0, 1000000, ErrorMessage = "Price must be between 0 and 1000000.")]
            public decimal price { get; set; }

            public string? description { get; set; }
            public bool? is_active { get; set; }
            public System.Collections.Generic.List<string> benefits { get; set; } = new System.Collections.Generic.List<string>();
            public int? sort_order { get; set; }
        }

        [HttpPost("plans/create")]
        public async Task<IActionResult> PlansCreateAsync([FromBody] PlanPayload payload, CancellationToken ct)
        {
            var result = await _billingService.CreatePlanAsync(payload, ct);
            if (!result.Success) return BadRequest(ApiResponse<object>.Fail(result.Message));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("plans/{pk}")]
        public async Task<IActionResult> PlanDetailAsync(long pk, CancellationToken ct)
        {
            var result = await _billingService.GetPlanDetailAsync(pk, ct);
            if (!result.Success) return NotFound(new { success = false, message = result.Message });
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPut("plans/{pk}")]
        public async Task<IActionResult> PlanUpdateAsync(long pk, [FromBody] PlanPayload payload, CancellationToken ct)
        {
            var result = await _billingService.UpdatePlanAsync(pk, payload, ct);
            if (!result.Success) return NotFound(new { success = false, message = result.Message });
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        public class TogglePayload
        {
            public bool? is_active { get; set; }
        }

        [HttpPatch("plans/{pk}/toggle")]
        public async Task<IActionResult> PlanToggleAsync(long pk, [FromBody] TogglePayload payload, CancellationToken ct)
        {
            var result = await _billingService.TogglePlanAsync(pk, payload, ct);
            if (!result.Success) return NotFound(new { success = false, message = result.Message });
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpDelete("plans/{pk}")]
        public async Task<IActionResult> PlanDeleteAsync(long pk, CancellationToken ct)
        {
            var result = await _billingService.DeletePlanAsync(pk, ct);
            if (!result.Success) return NotFound(new { success = false, message = result.Message });
            return Ok(ApiResponse<object>.Ok(new { }, "Plan deleted successfully"));
        }

        [HttpGet("plans/{pk}/students")]
        public async Task<IActionResult> PlanStudentsAsync(long pk, CancellationToken ct)
        {
            var result = await _billingService.GetPlanStudentsAsync(pk, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("memberships/expiring")]
        public async Task<IActionResult> MembershipsExpiringAsync([FromQuery] int days = 7, CancellationToken ct = default)
        {
            var result = await _billingService.GetExpiringMembershipsAsync(days, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("memberships/expired-today")]
        public async Task<IActionResult> MembershipsExpiredTodayAsync(CancellationToken ct)
        {
            var result = await _billingService.GetExpiredTodayMembershipsAsync(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        public class MembershipAssignPayload
        {
            public long student_id { get; set; }
            public long plan_id { get; set; }
            public string? start_date { get; set; }
            public string? end_date { get; set; }
            public string? notes { get; set; }
        }

        [HttpPost("memberships/assign")]
        public async Task<IActionResult> MembershipsAssignAsync([FromBody] MembershipAssignPayload payload, CancellationToken ct)
        {
            var result = await _billingService.AssignMembershipAsync(payload, ct);
            if (!result.Success) return NotFound(new { success = false, message = result.Message });
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("memberships/renew")]
        public async Task<IActionResult> MembershipsRenewAsync([FromBody] MembershipAssignPayload payload, CancellationToken ct)
        {
            var result = await _billingService.RenewMembershipAsync(payload, ct);
            if (!result.Success) return BadRequest(new { success = false, message = result.Message });
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("memberships/upgrade")]
        public async Task<IActionResult> MembershipsUpgradeAsync([FromBody] MembershipAssignPayload payload, CancellationToken ct)
        {
            var result = await _billingService.UpgradeMembershipAsync(payload, ct);
            if (!result.Success) return BadRequest(new { success = false, message = result.Message });
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("memberships")]
        public async Task<IActionResult> MembershipsListAsync([FromQuery] int page = 1, [FromQuery] int page_size = 10, [FromQuery] string search = "", [FromQuery] string status = "", [FromQuery] long? student_id = null, CancellationToken ct = default)
        {
            page_size = System.Math.Clamp(page_size, 1, 100);
            var nextTemplate = $"{Request.Scheme}://{Request.Host}/api/v1/admin/memberships?page={{P}}&page_size={page_size}";
            var prevTemplate = $"{Request.Scheme}://{Request.Host}/api/v1/admin/memberships?page={{P}}&page_size={page_size}";
            
            // Note: In previous version, we used relative path: "/api/v1/admin/memberships?page={P}&page_size={page_size}"
            var nextTemplateRel = $"/api/v1/admin/memberships?page={{P}}&page_size={page_size}";
            var prevTemplateRel = $"/api/v1/admin/memberships?page={{P}}&page_size={page_size}";

            var result = await _billingService.GetMembershipsListAsync(page, page_size, search ?? "", status ?? "", student_id, nextTemplateRel, prevTemplateRel, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("memberships/{pk}")]
        public async Task<IActionResult> MembershipDetailAsync(long pk, CancellationToken ct)
        {
            var result = await _billingService.GetMembershipDetailAsync(pk, ct);
            if (!result.Success) return NotFound(new { success = false, message = result.Message });
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("payments/summary")]
        public async Task<IActionResult> PaymentsSummaryAsync(CancellationToken ct)
        {
            var result = await _billingService.GetPaymentsSummaryAsync(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("payments/pending")]
        public async Task<IActionResult> PaymentsPendingAsync(CancellationToken ct)
        {
            var result = await _billingService.GetPendingPaymentsAsync(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("payments/overdue")]
        public async Task<IActionResult> PaymentsOverdueAsync(CancellationToken ct)
        {
            var result = await _billingService.GetOverduePaymentsAsync(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("payments")]
        public async Task<IActionResult> PaymentsListAsync([FromQuery] int page = 1, [FromQuery] int page_size = 10, [FromQuery] string search = "", [FromQuery] string status = "", CancellationToken ct = default)
        {
            page_size = System.Math.Clamp(page_size, 1, 100);
            var nextTemplateRel = $"/api/v1/admin/payments?page={{P}}&page_size={page_size}";
            var prevTemplateRel = $"/api/v1/admin/payments?page={{P}}&page_size={page_size}";

            var result = await _billingService.GetPaymentsListAsync(page, page_size, search ?? "", status ?? "", nextTemplateRel, prevTemplateRel, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        public class PaymentCreatePayload
        {
            public long student_id { get; set; }
            public long? plan_id { get; set; }
            public string duration_type { get; set; } = string.Empty;
            public int duration_days { get; set; }
            public string payment_mode { get; set; } = string.Empty;
            public string? transaction_ref { get; set; }
            public string? notes { get; set; }
        }

        [HttpPost("payments")]
        public async Task<IActionResult> PaymentsCreateAsync([FromBody] PaymentCreatePayload payload, CancellationToken ct)
        {
            var result = await _billingService.CreatePaymentAsync(payload, ct);
            if (!result.Success) return BadRequest(new { success = false, message = result.Message });
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("payments/{pk}")]
        public async Task<IActionResult> PaymentDetailAsync(long pk, CancellationToken ct)
        {
            var result = await _billingService.GetPaymentDetailAsync(pk, ct);
            if (!result.Success) return NotFound(new { success = false, message = result.Message });
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("payments/{pk}/verify")]
        public async Task<IActionResult> PaymentVerifyAsync(long pk, CancellationToken ct)
        {
            var result = await _billingService.VerifyPaymentAsync(pk, ct);
            if (!result.Success) return NotFound(new { success = false, message = result.Message });
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        public class RefundPayload
        {
            public decimal? refund_amount { get; set; }
            public string? refund_reason { get; set; }
        }

        [HttpPost("payments/{pk}/refund")]
        public async Task<IActionResult> PaymentRefundAsync(long pk, [FromBody] RefundPayload payload, CancellationToken ct)
        {
            var result = await _billingService.RefundPaymentAsync(pk, payload, ct);
            if (!result.Success) return NotFound(new { success = false, message = result.Message });
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        public class PaymentUpdateDto
        {
            public string? status { get; set; }
            public string? payment_mode { get; set; }
            public string? transaction_ref { get; set; }
            public string? notes { get; set; }
        }

        [HttpPut("payments/{pk}")]
        public async Task<IActionResult> PaymentUpdateAsync(long pk, [FromBody] PaymentUpdateDto payload, CancellationToken ct)
        {
            var result = await _billingService.UpdatePaymentAsync(pk, payload, ct);
            if (!result.Success) return NotFound(ApiResponse<object>.Fail(result.Message!));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("payments/{pk}/receipt")]
        public async Task<IActionResult> PaymentReceiptAsync(long pk, CancellationToken ct)
        {
            var result = await _billingService.GetPaymentReceiptAsync(pk, ct);
            if (!result.Success) return NotFound(new { success = false, message = result.Message });
            return Ok(ApiResponse<object>.Ok(result.Data));
        }
    }
}
