using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace InMemory
{
    public class AutoRefreshingCache<T>
    {
        private readonly MemoryCache _cache;
        private readonly int _expireAfterSeconds;
        private readonly int _refreshAfterSeconds;
        public readonly string CacheName;

        /// <summary>
        /// Create auto refreshable cache object
        /// </summary>
        /// <param name="expireAfter">expire after this argument value in seconds</param>
        /// <param name="refreshAfter">refresh after this argument value in seconds</param>
        /// <param name="cacheName">cacheName should be unique in the project and not containing # char</param>
        public AutoRefreshingCache(int expireAfter, int refreshAfter, string cacheName)
        {
            _cache = MemoryCache.Default;
            _expireAfterSeconds = expireAfter;
            _refreshAfterSeconds = refreshAfter;
            CacheName = cacheName;
        }

        /// <summary>
        /// Get the key value's and set by calculate method if is not exist in cache
        /// </summary>
        /// <param name="key">key, should not contain # char</param>
        /// <param name="calc"></param>
        /// <returns></returns>
        public T Get(string key, Func<T> calc)
        {
            var item = Get(key);
            if (item == null)
            {
                Refresh(key, calc);
                return Get(key).Value;
            }

            var refreshThreshold = DateTime.UtcNow.AddSeconds(-_refreshAfterSeconds);
            if (item.CalculationTime < refreshThreshold)
            {
                if (Interlocked.Increment(ref item.RefreshWorkers) == 1)
                {
                    Task.Run(() =>
                    {
                        Refresh(key, calc);
                    });
                }
            }
            return item.Value;
        }

        public int CountExpiredElements(IEnumerable<string> keys)
        {
            return keys?.Where(k => Get(k) == null).Count() ?? 0;
        }

        public void Inject(string key, T data)
        {
            var absoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(_expireAfterSeconds);
            lock (_cache)
            {
                _cache.Set($"{CacheName}#{key}", new CacheItemHolder(data), absoluteExpiration);
            }
        }

        private void Refresh(string key, Func<T> calc)
        {
            T data;
            try
            {
                data = calc();
            }
            catch
            {
                data = default(T);
            }
            Inject(key, data);
        }

        private CacheItemHolder Get(string key)
        {
            return _cache.Get($"{CacheName}#{key}") as CacheItemHolder;
        }

        private class CacheItemHolder
        {
            internal readonly T Value;
            internal int RefreshWorkers;
            internal readonly DateTime CalculationTime;

            public CacheItemHolder(T value)
            {
                Value = value;
                RefreshWorkers = 0;
                CalculationTime = DateTime.UtcNow;
            }
        }
    }
}