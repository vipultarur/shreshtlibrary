using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Data;

namespace WebApplication1.Middleware
{
    /// <summary>
    /// §1.4 — Validates the bearer token is not on the server-side revocation list.
    /// Prevents replay of tokens after logout/password change.
    /// Uses IMemoryCache to avoid hitting the database on every request.
    /// Revoked token hashes are cached for 5 minutes; the full revocation set
    /// is refreshed from the DB at most once per 30 seconds.
    /// </summary>
    public class TokenRevocationMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly TimeSpan RevokedSetRefreshInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan RevokedHashCacheDuration = TimeSpan.FromMinutes(5);
        private const string RevokedSetCacheKey = "RevokedTokenHashes";

        public TokenRevocationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext db, IMemoryCache cache)
        {
            // Only inspect authenticated requests that carry a Bearer token
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var rawToken = authHeader["Bearer ".Length..].Trim();

                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawToken));
                var tokenHash = Convert.ToHexString(hashBytes).ToLower();

                // Check against cached revocation set (O(1) HashSet lookup instead of DB query)
                var revokedSet = await GetOrRefreshRevokedSetAsync(cache, db);
                if (revokedSet.Contains(tokenHash))
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

        /// <summary>
        /// Returns a cached HashSet of revoked token hashes.
        /// Refreshes from the database at most once every 30 seconds.
        /// </summary>
        private static async Task<HashSet<string>> GetOrRefreshRevokedSetAsync(IMemoryCache cache, ApplicationDbContext db)
        {
            if (cache.TryGetValue(RevokedSetCacheKey, out HashSet<string>? cachedSet) && cachedSet != null)
            {
                return cachedSet;
            }

            // Load all active revoked token hashes from the database
            var revokedHashes = await db.AccountsAuthtokenrevocations
                .AsNoTracking()
                .Where(r => r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow)
                .Select(r => r.TokenHash)
                .ToListAsync();

            var hashSet = new HashSet<string>(revokedHashes, StringComparer.OrdinalIgnoreCase);

            cache.Set(RevokedSetCacheKey, hashSet, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = RevokedSetRefreshInterval,
            });

            return hashSet;
        }

        /// <summary>
        /// Call this static method when a new token is revoked (e.g., on logout)
        /// to immediately invalidate the cached set so the next request re-fetches from DB.
        /// </summary>
        public static void InvalidateCache(IMemoryCache cache)
        {
            cache.Remove(RevokedSetCacheKey);
        }
    }
}
