using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Http;
using WebApplication1.Models.Responses;
using WebApplication1.DTOs.Admin;

using WebApplication1.Utils;

namespace WebApplication1.Controllers
{
    [Route("api/v1/admin/sliders")]
    [ApiController]
    [Authorize(Roles = "admin,super_admin,sub_super_admin")]
    public class AdminSlidersController : ControllerBase
    {
        private readonly IAdminSlidersService _slidersService;

        public AdminSlidersController(IAdminSlidersService slidersService)
        {
            _slidersService = slidersService;
        }

        [HttpGet("")]
        [AuthorizePermission(Permissions.LibraryManagement.Slider)]
        public async Task<IActionResult> GetSliders(CancellationToken ct)
        {
            var result = await _slidersService.GetSliders(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("")]
        [AuthorizePermission(Permissions.LibraryManagement.Slider)]
        public async Task<IActionResult> CreateSlider([FromForm] SliderDto dto, CancellationToken ct)
        {
            var result = await _slidersService.CreateSlider(dto, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPut("{id}")]
        [AuthorizePermission(Permissions.LibraryManagement.Slider)]
        public async Task<IActionResult> UpdateSlider(long id, [FromForm] SliderDto dto, CancellationToken ct)
        {
            var result = await _slidersService.UpdateSlider(id, dto, ct);
            if (result.IsNotFound) return NotFound(ApiResponse<object>.Fail(result.Message));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpDelete("{id}")]
        [AuthorizePermission(Permissions.LibraryManagement.Slider)]
        public async Task<IActionResult> DeleteSlider(long id, CancellationToken ct)
        {
            var result = await _slidersService.DeleteSlider(id, ct);
            if (result.IsNotFound) return NotFound(ApiResponse<object>.Fail(result.Message));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }
    }
}
