using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Utils
{
    public class AuthorizePermissionAttribute : AuthorizeAttribute
    {
        public AuthorizePermissionAttribute(string permission)
        {
            Policy = $"Permission:{permission}";
        }
    }

    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }
        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }

    /// <summary>
    /// §1.2 — Re-derives role and permissions from the DB on every sensitive request.
    /// A tampered token claiming role:"super_admin" is useless here because we re-fetch
    /// the real permissions from the AccountsAdminusers table on every authorization check.
    /// This prevents JWT payload role escalation attacks.
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public PermissionAuthorizationHandler(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                return; // deny

            // §1.2: Re-fetch role from DB — never trust the JWT role claim for authz decisions
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WebApplication1.Data.ApplicationDbContext>();

            var adminUser = await db.AccountsAdminusers
                .AsNoTracking()
                .Select(u => new { u.Id, u.Role, u.Permissions, u.IsActive })
                .FirstOrDefaultAsync(u => u.Id == userId);

            // Deny if not found, or not an admin account, or deactivated
            if (adminUser == null || !adminUser.IsActive)
                return;

            var dbRole = adminUser.Role;

            // super_admin has all permissions (verified from DB, not token)
            if (dbRole == "super_admin")
            {
                context.Succeed(requirement);
                return;
            }

            // sub_super_admin has broad permissions (verified from DB)
            if (dbRole == "sub_super_admin")
            {
                context.Succeed(requirement);
                return;
            }

            // regular admin: check actual permissions string stored in DB (not token)
            if (dbRole == "admin")
            {
                var permissionsJson = adminUser.Permissions;
                if (!string.IsNullOrEmpty(permissionsJson))
                {
                    try
                    {
                        var permissions = System.Text.Json.JsonSerializer.Deserialize<string[]>(permissionsJson);
                        if (permissions != null && permissions.Contains(requirement.Permission))
                        {
                            context.Succeed(requirement);
                            return;
                        }
                    }
                    catch
                    {
                        // Malformed permissions JSON — deny access
                    }
                }
            }

            // Implicit deny — do NOT call context.Fail() so other handlers can still succeed
        }
    }
}
