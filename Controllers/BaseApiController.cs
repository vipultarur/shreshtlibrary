using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    public class BaseApiController : ControllerBase
    {
        protected readonly ICurrentUserService CurrentUserService;

        public BaseApiController(ICurrentUserService currentUserService)
        {
            CurrentUserService = currentUserService;
        }

        protected int? GetCurrentUserId()
        {
            return (int?)CurrentUserService.GetUserId();
        }

        protected bool TryGetUserId(out int userId)
        {
            var id = CurrentUserService.GetUserId();
            if (id.HasValue)
            {
                userId = (int)id.Value;
                return true;
            }
            userId = 0;
            return false;
        }

        protected IActionResult UnauthorizedResponse(string message)
        {
            return Unauthorized(new { success = false, status = "error", message = message });
        }
    }
}
