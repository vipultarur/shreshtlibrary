using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace WebApplication1.Services
{
    public interface ICurrentUserService
    {
        long? GetUserId();
        string? GetUserRole();
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public long? GetUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return null;

            var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("user_id");
            if (long.TryParse(userIdStr, out var userId))
            {
                return userId;
            }

            return null;
        }

        public string? GetUserRole()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role) ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("role");
        }
    }
}
