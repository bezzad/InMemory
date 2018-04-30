using System;
using System.Runtime.Caching;

namespace InMemory
{
    /// <summary>
    /// Use this class to put restriction on the number of invocations of an endpoint in your system by a given input.
    /// E.g., when a user wants to login with "test@test.com", call CanProceed("test@test.com") to check if she can do that.
    /// </summary>
    public class RateLimiter
    {
        private readonly MemoryCache _cache;
        private readonly string _cacheName; // should be unique in project
        private readonly int _maxTries;
        private readonly int _inPeriod; // seconds

        public RateLimiter(int maxTries, int inPeriod, string cacheName)
        {
            _cache = MemoryCache.Default;
            _cacheName = cacheName;
            _maxTries = maxTries;
            _inPeriod = inPeriod;
        }

        public bool CanProceed(string key)
        {
            var latestTries = _cache.Get($"{_cacheName}#{key}") as CacheItemHolder ?? new CacheItemHolder(_maxTries, _inPeriod * 5 / _maxTries);
            var absoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(_inPeriod);
            _cache.Set($"{_cacheName}#{key}", latestTries.RecordAccess(), absoluteExpiration);
            return latestTries.TryCounter < _maxTries;
        }

        private class CacheItemHolder
        {
            public int TryCounter { get; private set; }
            private DateTime _lastAccessTime;
            private readonly int _increaseAccessTimeOnSeconds;
            private readonly int _maxValue;

            public CacheItemHolder(int maxValue, int increaseAccessTimeOnSeconds)
            {
                TryCounter = 0;
                _lastAccessTime = DateTime.UtcNow;
                _increaseAccessTimeOnSeconds = increaseAccessTimeOnSeconds;
                _maxValue = maxValue;
            }

            public CacheItemHolder RecordAccess()
            {
                var timeDiff = (DateTime.Now - _lastAccessTime).TotalSeconds;
                if (timeDiff < _increaseAccessTimeOnSeconds)
                {
                    if (TryCounter < _maxValue)
                        TryCounter++;
                }
                else
                {
                    TryCounter = Math.Max((int)(TryCounter - (timeDiff / _increaseAccessTimeOnSeconds)), 0);
                }
                _lastAccessTime = DateTime.Now;
                return this;
            }
        }
    }
}
