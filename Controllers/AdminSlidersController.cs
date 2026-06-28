using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Http;
using WebApplication1.Models.Responses;
using WebApplication1.DTOs.Admin;

namespace WebApplication1.Controllers
{
    [Route("api/v1/admin/sliders")]
    [ApiController]
    [Authorize(Roles = "admin,super_admin")]
    public class AdminSlidersController : ControllerBase
    {
        private readonly IAdminSlidersService _slidersService;

        public AdminSlidersController(IAdminSlidersService slidersService)
        {
            _slidersService = slidersService;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetSliders(CancellationToken ct)
        {
            var result = await _slidersService.GetSliders(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("")]
        public async Task<IActionResult> CreateSlider([FromForm] SliderDto dto, CancellationToken ct)
        {
            var result = await _slidersService.CreateSlider(dto, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSlider(long id, [FromForm] SliderDto dto, CancellationToken ct)
        {
            var result = await _slidersService.UpdateSlider(id, dto, ct);
            if (result.IsNotFound) return NotFound(ApiResponse<object>.Fail(result.Message));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSlider(long id, CancellationToken ct)
        {
            var result = await _slidersService.DeleteSlider(id, ct);
            if (result.IsNotFound) return NotFound(ApiResponse<object>.Fail(result.Message));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }
    }
}
