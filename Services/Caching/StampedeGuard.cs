using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication1.Services.Caching
{
    /// <summary>
    /// Prevents cache stampedes (thundering herd problem) by wrapping fallback calls in per-key single-flight locks.
    /// When a cache key expires under heavy load, only ONE request executes the factory database query.
    /// Concurrent requests wait for the lock and then serve the freshly populated result.
    /// </summary>
    public class StampedeGuard
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public async Task<T?> GetOrCreateAsync<T>(
            string key,
            Func<Task<T?>> factory,
            ITieredCache cache,
            TimeSpan ttl)
        {
            var cached = await cache.GetAsync<T>(key);
            if (cached != null) return cached;

            var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                // Double-check cache after acquiring lock in case a previous request populated it
                cached = await cache.GetAsync<T>(key);
                if (cached != null) return cached;

                var fresh = await factory();
                if (fresh != null)
                {
                    await cache.SetAsync(key, fresh, ttl);
                }
                return fresh;
            }
            finally
            {
                gate.Release();
            }
        }
    }
}
