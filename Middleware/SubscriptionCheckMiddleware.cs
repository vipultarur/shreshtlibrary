using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebApplication1.Data;

namespace WebApplication1.Middleware;

public class SubscriptionCheckMiddleware
{
    private readonly RequestDelegate _next;

    public SubscriptionCheckMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Only protect API routes
        if (!path.StartsWith("/api/"))
        {
            await _next(context);
            return;
        }

        // Allow auth and public endpoints
        if (path.StartsWith("/api/v1/auth/") || 
            path.StartsWith("/api/v1/public/") || 
            path.StartsWith("/api/v1/webhook/") ||
            path.StartsWith("/api/v1/library/info"))
        {
            await _next(context);
            return;
        }

        // Fetch user role
        var roleClaim = context.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        
        // If not authenticated yet, let the Auth middleware handle it
        if (string.IsNullOrEmpty(roleClaim))
        {
            await _next(context);
            return;
        }

        // Super Admin bypasses all subscription checks
        if (roleClaim == "super_admin")
        {
            await _next(context);
            return;
        }

        // Check the latest subscription for the library
        var latestSubscription = await dbContext.LibrarySubscriptions
            .OrderByDescending(s => s.ExpiryDate)
            .FirstOrDefaultAsync();

        bool isExpired = latestSubscription == null || latestSubscription.Status == "Expired" || latestSubscription.ExpiryDate < DateTime.UtcNow;

        if (isExpired)
        {
            // Allow sub_super_admin to access licensing endpoints to renew
            if (roleClaim == "sub_super_admin" && path.StartsWith("/api/v1/licensing/"))
            {
                await _next(context);
                return;
            }

            // Block everything else with 403
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            
            var response = new
            {
                success = false,
                code = "LIBRARY_SUBSCRIPTION_EXPIRED",
                message = "The library's subscription has expired. Services are temporarily unavailable."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            return;
        }

        await _next(context);
    }
}
