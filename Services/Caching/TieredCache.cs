using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace WebApplication1.Services.Caching
{
    public interface ITieredCache
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan ttl);
        Task RemoveAsync(string key);
    }

    /// <summary>
    /// Unified two-tier cache combining L1 (IMemoryCache) and L2 (SqliteDiskCache).
    /// </summary>
    public class TieredCache : ITieredCache
    {
        private readonly IMemoryCache _l1;
        private readonly SqliteDiskCache _l2;
        private readonly ILogger<TieredCache> _logger;

        public TieredCache(IMemoryCache l1, SqliteDiskCache l2, ILogger<TieredCache> logger)
        {
            _l1 = l1;
            _l2 = l2;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            if (_l1.TryGetValue(key, out T? l1Value) && l1Value != null)
            {
                CacheMetrics.RecordL1Hit();
                return l1Value;
            }

            var l2Value = await _l2.GetAsync<T>(key);
            if (l2Value != null)
            {
                CacheMetrics.RecordL2Hit();
                // Promote hit from L2 disk cache to L1 RAM cache for fast subsequent lookups
                _l1.Set(key, l2Value, TimeSpan.FromMinutes(5));
                return l2Value;
            }

            CacheMetrics.RecordMiss();
            return default;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
        {
            if (value == null) return;
            _l1.Set(key, value, ttl);
            await _l2.SetAsync(key, value, ttl);
        }

        public async Task RemoveAsync(string key)
        {
            _l1.Remove(key);
            await _l2.RemoveAsync(key);
        }
    }
}
