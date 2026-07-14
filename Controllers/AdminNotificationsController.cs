using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.Services;

using WebApplication1.Utils;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Roles = "admin,super_admin,sub_super_admin")]
    public class AdminNotificationsController : ControllerBase
    {
        private readonly IAdminNotificationService _adminNotificationService;

        public AdminNotificationsController(IAdminNotificationService adminNotificationService)
        {
            _adminNotificationService = adminNotificationService;
        }

        [HttpGet("notifications/templates")]
        [AuthorizePermission(Permissions.NotificationManagement.Send)]
        public async Task<IActionResult> NotificationTemplatesAsync(CancellationToken ct)
        {
            var result = await _adminNotificationService.GetNotificationTemplatesAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("notifications/scheduled")]
        [AuthorizePermission(Permissions.NotificationManagement.Send)]
        public async Task<IActionResult> NotificationScheduledAsync(CancellationToken ct)
        {
            var result = await _adminNotificationService.GetScheduledNotificationsAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpDelete("notifications/scheduled/{pk}/cancel")]
        [AuthorizePermission(Permissions.NotificationManagement.Send)]
        public async Task<IActionResult> NotificationScheduledCancelAsync(int pk, CancellationToken ct)
        {
            var result = await _adminNotificationService.CancelScheduledNotificationAsync(pk, ct);
            if (result.IsNotFound) return NotFound(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data, result.Message));
        }

        [HttpPost("notifications/schedule")]
        [AuthorizePermission(Permissions.NotificationManagement.Send)]
        public async Task<IActionResult> NotificationScheduleAsync([FromForm] NotificationPayloadDto dto, CancellationToken ct)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Where(x => x.Value?.Errors.Count > 0)
                        .ToDictionary(x => x.Key, x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
                    return BadRequest(new { success = false, message = "Validation failed", errors });
                }
                var result = await _adminNotificationService.ProcessNotificationAsync(dto, isSchedule: true, ct);
                return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
            }
            catch (Exception ex)
            {
                return BadRequest(WebApplication1.Models.Responses.ApiResponse<object>.Fail($"Failed to schedule notification: {ex.Message}"));
            }
        }

        [HttpPost("notifications/send")]
        [AuthorizePermission(Permissions.NotificationManagement.Send)]
        public async Task<IActionResult> NotificationSendAsync([FromForm] NotificationPayloadDto dto, CancellationToken ct)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Where(x => x.Value?.Errors.Count > 0)
                        .ToDictionary(x => x.Key, x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
                    return BadRequest(new { success = false, message = "Validation failed", errors });
                }
                var result = await _adminNotificationService.ProcessNotificationAsync(dto, isSchedule: false, ct);
                return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
            }
            catch (Exception ex)
            {
                return BadRequest(WebApplication1.Models.Responses.ApiResponse<object>.Fail($"Failed to send notification: {ex.Message}"));
            }
        }

        [HttpGet("notifications")]
        [AuthorizePermission(Permissions.NotificationManagement.View)]
        public async Task<IActionResult> NotificationsListAsync(CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int page_size = 50)
        {
            page_size = Math.Clamp(page_size, 1, 200);
            page = Math.Max(1, page);
            var result = await _adminNotificationService.GetNotificationsListAsync(page, page_size, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("notifications/{pk}")]
        [AuthorizePermission(Permissions.NotificationManagement.View)]
        public async Task<IActionResult> NotificationDetailAsync(int pk, CancellationToken ct)
        {
            var result = await _adminNotificationService.GetNotificationDetailAsync(pk, ct);
            if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("notifications/{pk}/recipients")]
        [AuthorizePermission(Permissions.NotificationManagement.Send)]
        public async Task<IActionResult> NotificationRecipientsAsync(int pk, CancellationToken ct)
        {
            var result = await _adminNotificationService.GetNotificationRecipientsAsync(pk, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("inbox")]
        [AuthorizePermission(Permissions.NotificationManagement.Send)]
        public async Task<IActionResult> InboxNotificationsAsync(CancellationToken ct)
        {
            var result = await _adminNotificationService.GetInboxNotificationsAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("inbox/{pk}/{actionType}")]
        [AuthorizePermission(Permissions.NotificationManagement.Send)]
        public async Task<IActionResult> InboxActionAsync(long pk, string actionType, CancellationToken ct)
        {
            var result = await _adminNotificationService.MarkInboxActionAsync(pk, actionType, ct);
            if (result.IsNotFound) return NotFound(new { message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { message = "Action completed" }));
        }

        [HttpDelete("inbox/{pk}")]
        [AuthorizePermission(Permissions.NotificationManagement.Send)]
        public async Task<IActionResult> InboxDeleteAsync(long pk, CancellationToken ct)
        {
            var result = await _adminNotificationService.DeleteInboxNotificationAsync(pk, ct);
            if (result.IsNotFound) return NotFound(new { message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { message = "Deleted successfully" }));
        }
    }
}
