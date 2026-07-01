using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Roles = "admin,super_admin")]
    public class AdminNotificationsController : ControllerBase
    {
        private readonly IAdminNotificationService _adminNotificationService;

        public AdminNotificationsController(IAdminNotificationService adminNotificationService)
        {
            _adminNotificationService = adminNotificationService;
        }

        [HttpGet("notifications/templates")]
        public async Task<IActionResult> NotificationTemplatesAsync(CancellationToken ct)
        {
            var result = await _adminNotificationService.GetNotificationTemplatesAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("notifications/scheduled")]
        public async Task<IActionResult> NotificationScheduledAsync(CancellationToken ct)
        {
            var result = await _adminNotificationService.GetScheduledNotificationsAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpDelete("notifications/scheduled/{pk}/cancel")]
        public async Task<IActionResult> NotificationScheduledCancelAsync(int pk, CancellationToken ct)
        {
            var result = await _adminNotificationService.CancelScheduledNotificationAsync(pk, ct);
            var dynamicData = (dynamic)result.Data;
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { }, dynamicData?.message));
        }

        [HttpPost("notifications/schedule")]
        public async Task<IActionResult> NotificationScheduleAsync([FromForm] NotificationPayloadDto dto, CancellationToken ct)
        {
            var result = await _adminNotificationService.ProcessNotificationAsync(dto, isSchedule: true, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("notifications/send")]
        public async Task<IActionResult> NotificationSendAsync([FromForm] NotificationPayloadDto dto, CancellationToken ct)
        {
            var result = await _adminNotificationService.ProcessNotificationAsync(dto, isSchedule: false, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> NotificationsListAsync(CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int page_size = 50)
        {
            var result = await _adminNotificationService.GetNotificationsListAsync(page, page_size, ct);
            var dynamicData = (dynamic)result.Data;
            return Ok(new { 
                success = true, 
                status = "success", 
                data = dynamicData?.data, 
                count = dynamicData?.count, 
                total_pages = dynamicData?.total_pages, 
                current_page = dynamicData?.current_page 
            });
        }

        [HttpGet("notifications/{pk}")]
        public async Task<IActionResult> NotificationDetailAsync(int pk, CancellationToken ct)
        {
            var result = await _adminNotificationService.GetNotificationDetailAsync(pk, ct);
            if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("notifications/{pk}/recipients")]
        public async Task<IActionResult> NotificationRecipientsAsync(int pk, CancellationToken ct)
        {
            var result = await _adminNotificationService.GetNotificationRecipientsAsync(pk, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("inbox")]
        public async Task<IActionResult> InboxNotificationsAsync(CancellationToken ct)
        {
            var result = await _adminNotificationService.GetInboxNotificationsAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("inbox/{pk}/{action}")]
        public async Task<IActionResult> InboxActionAsync(long pk, string action, CancellationToken ct)
        {
            var result = await _adminNotificationService.MarkInboxActionAsync(pk, action, ct);
            if (result.IsNotFound) return NotFound(new { message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { message = "Action completed" }));
        }

        [HttpDelete("inbox/{pk}")]
        public async Task<IActionResult> InboxDeleteAsync(long pk, CancellationToken ct)
        {
            var result = await _adminNotificationService.DeleteInboxNotificationAsync(pk, ct);
            if (result.IsNotFound) return NotFound(new { message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { message = "Deleted successfully" }));
        }
    }
}
