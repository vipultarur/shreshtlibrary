using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Threading;
using WebApplication1.Services;
using WebApplication1.Models.Responses;
using WebApplication1.Models.DTOs.Notifications;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/notifications")]
    [Authorize]
    public class NotificationsController : BaseApiController
    {
        private readonly IStudentNotificationService _notificationService;

        public NotificationsController(IStudentNotificationService notificationService, ICurrentUserService currentUserService) : base(currentUserService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("list")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetNotificationsAsync([FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _notificationService.GetNotificationsAsync(userId.Value, page, page_size, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("read/{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> MarkNotificationReadAsync(int id, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _notificationService.MarkNotificationReadAsync(userId.Value, id, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("read-all")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> MarkAllReadAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _notificationService.MarkAllReadAsync(userId.Value, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> DeleteNotificationAsync(int id, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _notificationService.DeleteNotificationAsync(userId.Value, id, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpDelete("all")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> DeleteAllNotificationsAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _notificationService.DeleteAllNotificationsAsync(userId.Value, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("register-device")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> RegisterDeviceAsync([FromBody] DeviceTokenPayload payload, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _notificationService.RegisterDeviceAsync(userId.Value, payload, ct);
            if (!result.Success) return BadRequest(ApiResponse<object>.Fail(result.Message!));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }
    }
}
