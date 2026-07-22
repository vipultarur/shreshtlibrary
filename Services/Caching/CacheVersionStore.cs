using System.Collections.Concurrent;

namespace WebApplication1.Services.Caching
{
    /// <summary>
    /// Maintains monotonic integer version counters per entity type.
    /// Invalidation bumps the version counter, which naturally makes old cache keys unreachable
    /// without requiring a synchronous hard delete or facing read-after-delete race conditions.
    /// </summary>
    public class CacheVersionStore
    {
        private readonly ConcurrentDictionary<string, int> _versions = new(System.StringComparer.OrdinalIgnoreCase);

        public int GetVersion(string entityType)
        {
            return _versions.GetOrAdd(entityType, 1);
        }

        public int BumpVersion(string entityType)
        {
            return _versions.AddOrUpdate(entityType, 2, (_, currentVersion) => currentVersion + 1);
        }

        public ConcurrentDictionary<string, int> GetAllVersions()
        {
            return _versions;
        }
    }
}
