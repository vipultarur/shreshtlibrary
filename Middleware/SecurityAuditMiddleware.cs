using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace WebApplication1.Middleware
{
    /// <summary>
    /// §1.9 — Logs all 401/403 responses with user id, IP, and path for SIEM/anomaly alerting.
    /// </summary>
    public class SecurityAuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityAuditMiddleware> _logger;

        public SecurityAuditMiddleware(RequestDelegate next, ILogger<SecurityAuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            var status = context.Response.StatusCode;
            if (status == 401 || status == 403)
            {
                var userId = context.User?.FindFirst("user_id")?.Value ?? "anonymous";
                var role = context.User?.FindFirst("role")?.Value ?? "none";
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var method = context.Request.Method;
                var path = context.Request.Path.Value;

                if (status == 401)
                    _logger.LogWarning(
                        "[SECURITY] AUTH_FAILURE | UserId={UserId} Role={Role} IP={IP} {Method} {Path}",
                        userId, role, ip, method, path);
                else
                    _logger.LogWarning(
                        "[SECURITY] AUTHZ_FAILURE (403) | UserId={UserId} Role={Role} IP={IP} {Method} {Path}",
                        userId, role, ip, method, path);
            }
        }
    }
}
