using Microsoft.AspNetCore.Authorization;
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

    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var roleClaim = context.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            if (string.IsNullOrEmpty(roleClaim))
            {
                return Task.CompletedTask;
            }

            // Super Admin has all permissions
            if (roleClaim == "super_admin")
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Sub Super Admin has all permissions EXCEPT AdminManagement (or specific ones if restricted further, but per requirements they can manage normal admins)
            // They cannot manage Super Admins or change Super Admin permissions, which is handled at the service level, but for UI/endpoints they have broad access.
            if (roleClaim == "sub_super_admin")
            {
                // Optionally restrict some maintenance/security if needed, but requirements say "Can access almost every system feature"
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Admin: check specific permissions claim
            if (roleClaim == "admin")
            {
                var permissionsClaim = context.User.Claims.FirstOrDefault(c => c.Type == "permissions")?.Value;
                if (!string.IsNullOrEmpty(permissionsClaim))
                {
                    try
                    {
                        var permissions = System.Text.Json.JsonSerializer.Deserialize<string[]>(permissionsClaim);
                        if (permissions != null && permissions.Contains(requirement.Permission))
                        {
                            context.Succeed(requirement);
                            return Task.CompletedTask;
                        }
                    }
                    catch
                    {
                        // JSON parsing failed, deny access
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
