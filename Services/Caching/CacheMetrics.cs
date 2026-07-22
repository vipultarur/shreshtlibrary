using System.Threading;

namespace WebApplication1.Services.Caching
{
    /// <summary>
    /// Atomic in-process metrics tracker for cache hits and misses across L1 and L2 tiers.
    /// </summary>
    public static class CacheMetrics
    {
        private static long _l1Hits;
        private static long _l2Hits;
        private static long _misses;

        public static void RecordL1Hit() => Interlocked.Increment(ref _l1Hits);
        public static void RecordL2Hit() => Interlocked.Increment(ref _l2Hits);
        public static void RecordMiss() => Interlocked.Increment(ref _misses);

        public static object Snapshot()
        {
            var l1 = Interlocked.Read(ref _l1Hits);
            var l2 = Interlocked.Read(ref _l2Hits);
            var misses = Interlocked.Read(ref _misses);
            var totalHits = l1 + l2;
            var totalRequests = totalHits + misses;
            var hitRatio = totalRequests == 0 ? 0.0 : (double)totalHits / totalRequests;

            return new
            {
                l1_hits = l1,
                l2_hits = l2,
                total_hits = totalHits,
                misses = misses,
                total_requests = totalRequests,
                hit_ratio = System.Math.Round(hitRatio, 4),
                hit_ratio_percentage = $"{System.Math.Round(hitRatio * 100, 2)}%"
            };
        }
    }
}
