using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.Services;
using WebApplication1.Models.DTOs.Admin;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/admin/students")]
    [Authorize(Roles = "admin,super_admin")]
    public class AdminStudentsController : ControllerBase
    {
        private readonly IStudentAdminService _studentAdminService;

        public AdminStudentsController(IStudentAdminService studentAdminService)
        {
            _studentAdminService = studentAdminService;
        }

        [HttpGet("counts")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> GetStudentCountsAsync(CancellationToken ct)
        {
            var result = await _studentAdminService.GetStudentCountsAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportStudentsAsync(CancellationToken ct)
        {
            var result = await _studentAdminService.ExportStudentsAsync(ct);
            if (result.Data is byte[] bytes)
            {
                return File(bytes, "text/csv", "students.csv");
            }
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> ManageStudentsGetAsync(CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int page_size = 10, [FromQuery] string search = "", [FromQuery] string status = "")
        {
            page_size = Math.Clamp(page_size, 1, 100);
            page = Math.Max(1, page);
            var result = await _studentAdminService.GetStudentsAsync(page, page_size, search, status, Request.Scheme, Request.Host.Value, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("{pk}")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> GetStudentDetailAsync(string pk, CancellationToken ct)
        {
            var result = await _studentAdminService.GetStudentDetailAsync(pk, Request.Scheme, Request.Host.Value, ct);
            if (result.IsNotFound) return NotFound(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }


        [HttpPost]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 201)]
        public async Task<IActionResult> CreateStudentAsync([FromBody] StudentPayload payload, CancellationToken ct)
        {
            var result = await _studentAdminService.CreateStudentAsync(payload, ct);
            if (!result.Success)
            {
                return BadRequest(WebApplication1.Models.Responses.ApiResponse<object>.Fail("Validation error", result.Errors));
            }

            return Created($"/api/v1/admin/students/{(result.Data as dynamic)?.id ?? "new"}",
                WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPut("{pk}")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> UpdateStudentAsync(string pk, [FromBody] StudentPayload payload, CancellationToken ct)
        {
            var result = await _studentAdminService.UpdateStudentAsync(pk, payload, ct);
            
            if (result.IsNotFound) return NotFound(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));
            if (!result.Success) return BadRequest(WebApplication1.Models.Responses.ApiResponse<object>.Fail("Validation error", result.Errors));

            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpDelete("{pk}")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> DeleteStudentAsync(string pk, CancellationToken ct)
        {
            var result = await _studentAdminService.DeleteStudentAsync(pk, ct);
            if (result.IsNotFound) return NotFound(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));

            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(null, "Student deleted successfully."));
        }

        [HttpPost("{pk}/photo")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> UploadStudentPhotoAsync(string pk, Microsoft.AspNetCore.Http.IFormFile profile_photo, CancellationToken ct)
        {
            var result = await _studentAdminService.UploadStudentPhotoAsync(pk, profile_photo, Request.Scheme, Request.Host.Value, ct);
            if (result.IsNotFound) return NotFound(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));
            if (!result.Success) return BadRequest(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));

            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data, "Photo uploaded successfully"));
        }

        [HttpGet("{pk}/analytics")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> GetStudentAnalyticsAsync(string pk, CancellationToken ct)
        {
            var result = await _studentAdminService.GetStudentAnalyticsAsync(pk, ct);
            if (result.IsNotFound) return NotFound(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));

            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }


        [HttpPost("{pk}/suspend")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> SuspendStudentAsync(string pk, [FromBody] SuspendStudentRequest request, CancellationToken ct)
        {
            var result = await _studentAdminService.SuspendStudentAsync(pk, request?.Reason, ct);
            if (result.IsNotFound) return NotFound(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));

            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("{pk}/activate")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> ActivateStudentAsync(string pk, CancellationToken ct)
        {
            var result = await _studentAdminService.ActivateStudentAsync(pk, ct);
            if (result.IsNotFound) return NotFound(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));

            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }
        
        [HttpGet("{pk}/{kind}")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> GetStudentRelatedDataAsync(string pk, string kind, CancellationToken ct)
        {
            var result = await _studentAdminService.GetStudentRelatedDataAsync(pk, kind, ct);
            if (result.IsNotFound) return NotFound(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));
            if (!result.Success) return BadRequest(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));

            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }
    }
}
