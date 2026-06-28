using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Models.DTOs;
using WebApplication1.Services;
using WebApplication1.Models.Responses;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/admin/settings")]
    [Authorize(Roles = "admin,super_admin")]
    public class AdminSettingsController : ControllerBase
    {
        private readonly IAdminSettingsService _settingsService;

        public AdminSettingsController(IAdminSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings(CancellationToken ct)
        {
            var result = await _settingsService.GetSettingsAsync(ct);
            if (!result.Success) return BadRequest(ApiResponse<object>.Fail(result.Message ?? "Error"));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] SettingsPayload payload, CancellationToken ct)
        {
            var result = await _settingsService.UpdateSettingsAsync(payload, ct);
            if (!result.Success) return BadRequest(ApiResponse<object>.Fail(result.Message ?? "Error"));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }
    }
}
