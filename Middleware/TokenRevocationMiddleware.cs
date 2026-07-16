using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Data;

namespace WebApplication1.Middleware
{
    /// <summary>
    /// §1.4 — Validates the bearer token is not on the server-side revocation list.
    /// Prevents replay of tokens after logout/password change.
    /// </summary>
    public class TokenRevocationMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenRevocationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
        {
            // Only inspect authenticated requests that carry a Bearer token
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var rawToken = authHeader["Bearer ".Length..].Trim();

                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawToken));
                var tokenHash = Convert.ToHexString(hashBytes).ToLower();

                var isRevoked = await db.AccountsAuthtokenrevocations
                    .AsNoTracking()
                    .AnyAsync(r =>
                        r.TokenHash == tokenHash &&
                        (r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow));

                if (isRevoked)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        "{\"success\":false,\"status\":\"error\",\"message\":\"Token has been revoked.\"}");
                    return;
                }
            }

            await _next(context);
        }
    }
}
