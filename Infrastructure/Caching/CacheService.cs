using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MyApi.Infrastructure.Caching
{
    /// <summary>
    /// Interface for distributed caching service
    /// Supports automatic cache expiration and pattern-based invalidation
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Gets a value from cache or executes factory if not found
        /// </summary>
        /// <typeparam name="T">Type of cached value</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="factory">Async factory function to get value if not cached</param>
        /// <param name="expiration">Optional custom expiration time</param>
        /// <returns>Cached or freshly fetched value</returns>
        Task<T?> GetOrSetAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiration = null);

        /// <summary>
        /// Directly get from cache without factory
        /// </summary>
        T? Get<T>(string key);

        /// <summary>
        /// Directly set cache value
        /// </summary>
        void Set<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// Remove single cache entry
        /// </summary>
        void Remove(string key);

        /// <summary>
        /// Remove all cache entries matching pattern
        /// </summary>
        void RemoveByPattern(string pattern);

        /// <summary>
        /// Clear all cache
        /// </summary>
        void Clear();

        /// <summary>
        /// Get cache statistics
        /// </summary>
        CacheStats GetStats();
    }

    /// <summary>
    /// Cache statistics for monitoring
    /// </summary>
    public class CacheStats
    {
        public int TotalKeys { get; set; }
        public int Hits { get; set; }
        public int Misses { get; set; }
        public double HitRate => Hits + Misses > 0 ? (double)Hits / (Hits + Misses) : 0;
    }

    /// <summary>
    /// In-memory cache service implementation
    /// Thread-safe and supports pattern-based invalidation
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;

        // Track all cache keys for pattern matching
        private static readonly ConcurrentDictionary<string, CacheKeyMetadata> _cacheKeys
            = new();

        private int _hits = 0;
        private int _misses = 0;

        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Get or set cache with automatic factory execution
        /// </summary>
        public async Task<T?> GetOrSetAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiration = null)
        {
            // Try to get from cache
            if (_cache.TryGetValue(key, out T? cachedValue))
            {
                _hits++;
                _logger.LogDebug("✓ Cache HIT: {Key}", key);
                return cachedValue;
            }

            _misses++;
            _logger.LogDebug("✗ Cache MISS: {Key}, fetching from source", key);

            // Not in cache, execute factory
            var value = await factory();

            // Set cache with expiration
            var cacheOptions = new MemoryCacheEntryOptions
            {
                Size = 1  // Each entry counts as 1 toward SizeLimit
            };

            if (expiration.HasValue)
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = expiration;
            }
            else
            {
                // Default: 10 minute sliding expiration
                cacheOptions.SlidingExpiration = TimeSpan.FromMinutes(10);
            }

            _cache.Set(key, value, cacheOptions);

            // Track key for pattern matching
            _cacheKeys.TryAdd(key, new CacheKeyMetadata
            {
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(10))
            });

            _logger.LogDebug("→ Cache SET: {Key} (expires in {Expiration})",
                key,
                expiration?.TotalMinutes ?? 10);

            return value;
        }

        /// <summary>
        /// Direct get from cache
        /// </summary>
        public T? Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                _hits++;
                _logger.LogDebug("✓ Cache GET HIT: {Key}", key);
                return value;
            }

            _misses++;
            _logger.LogDebug("✗ Cache GET MISS: {Key}", key);
            return default;
        }

        /// <summary>
        /// Direct set to cache
        /// </summary>
        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            var options = new MemoryCacheEntryOptions
            {
                Size = 1  // Each entry counts as 1 toward SizeLimit
            };

            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration;
            }
            else
            {
                options.SlidingExpiration = TimeSpan.FromMinutes(10);
            }

            _cache.Set(key, value, options);

            _cacheKeys.TryAdd(key, new CacheKeyMetadata
            {
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(10))
            });

            _logger.LogDebug("→ Cache SET: {Key}", key);
        }

        /// <summary>
        /// Remove single cache entry
        /// </summary>
        public void Remove(string key)
        {
            _cache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
            _logger.LogInformation("🗑️ Cache REMOVED: {Key}", key);
        }

        /// <summary>
        /// Remove all cache entries matching pattern
        /// Example: RemoveByPattern("contact_tags_*")
        /// </summary>
        public void RemoveByPattern(string pattern)
        {
            var keysToRemove = _cacheKeys.Keys
                .Where(k => PatternMatches(k, pattern))
                .ToList();

            foreach (var key in keysToRemove)
            {
                Remove(key);
            }

            _logger.LogInformation("🗑️ Cache CLEARED pattern '{Pattern}': {Count} keys removed",
                pattern, keysToRemove.Count);
        }

        /// <summary>
        /// Clear all cache entries
        /// </summary>
        public void Clear()
        {
            var allKeys = _cacheKeys.Keys.ToList();
            foreach (var key in allKeys)
            {
                _cache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
            }

            _hits = 0;
            _misses = 0;

            _logger.LogInformation("🗑️ Cache CLEARED: all entries removed");
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public CacheStats GetStats()
        {
            return new CacheStats
            {
                TotalKeys = _cacheKeys.Count,
                Hits = _hits,
                Misses = _misses
            };
        }

        /// <summary>
        /// Simple pattern matching (* = wildcard)
        /// </summary>
        private static bool PatternMatches(string key, string pattern)
        {
            if (pattern.Contains('*'))
            {
                var parts = pattern.Split('*');
                var str = key;

                foreach (var part in parts)
                {
                    if (string.IsNullOrEmpty(part))
                        continue;

                    var idx = str.IndexOf(part);
                    if (idx < 0)
                        return false;

                    str = str.Substring(idx + part.Length);
                }

                return true;
            }

            return key.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Metadata about cached keys
        /// </summary>
        private class CacheKeyMetadata
        {
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }

    /// <summary>
    /// Cache invalidation helper for distributed systems
    /// </summary>
    public class CacheInvalidationHelper
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<CacheInvalidationHelper> _logger;

        public CacheInvalidationHelper(
            ICacheService cacheService,
            ILogger<CacheInvalidationHelper> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Invalidate contact-related caches
        /// </summary>
        public void InvalidateContactCaches()
        {
            _cacheService.RemoveByPattern("contact_*");
            _logger.LogInformation("Invalidated all contact caches");
        }

        /// <summary>
        /// Invalidate specific contact cache
        /// </summary>
        public void InvalidateContact(int contactId)
        {
            _cacheService.Remove($"contact_{contactId}");
            _cacheService.RemoveByPattern("contact_list_*");
            _logger.LogInformation("Invalidated contact {ContactId} caches", contactId);
        }

        /// <summary>
        /// Invalidate lookup/reference data caches
        /// </summary>
        public void InvalidateLookups()
        {
            _cacheService.RemoveByPattern("lookup_*");
            _logger.LogInformation("Invalidated all lookup caches");
        }

        /// <summary>
        /// Invalidate specific lookup type
        /// </summary>
        public void InvalidateLookupType(string lookupType)
        {
            _cacheService.Remove($"lookup_{lookupType}");
            _logger.LogInformation("Invalidated lookup type '{LookupType}' cache", lookupType);
        }

        /// <summary>
        /// Invalidate dispatch caches
        /// </summary>
        public void InvalidateDispatches()
        {
            _cacheService.RemoveByPattern("dispatch_*");
            _logger.LogInformation("Invalidated all dispatch caches");
        }

        /// <summary>
        /// Invalidate service order caches
        /// </summary>
        public void InvalidateServiceOrders()
        {
            _cacheService.RemoveByPattern("serviceorder_*");
            _logger.LogInformation("Invalidated all service order caches");
        }
    }
}
