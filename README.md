# InMemory Cache

[![Build status](https://ci.appveyor.com/api/projects/status/vpt1d9biulupim04?svg=true)](https://ci.appveyor.com/project/Behzadkhosravifar/inmemory)
[![nuget version](https://img.shields.io/nuget/v/inmemory.svg)](https://www.nuget.org/packages/InMemory)
[![Nuget downloads](http://img.shields.io/nuget/dt/inmemory.svg)](https://www.nuget.org/packages/inmemory/)

----------------------------------------------------

A MemoryCache which tries to prevent cache miss for hot entries, by refreshing before expiration.

### Auto Refreshing Cache example:

```C#

// define auto refreshing cache
var cache = new AutoRefreshingCache<string>(expireAfter: 10, refreshAfter: 8, cacheName: "shortTimeCache");

// add key/value data to cache
cache.Inject("key", "value");

// get count of expired cache by key elements
int expiredCacheCount = cache.CountExpiredElements(new[] { "key1", "key2", "key3", "key4" });

// get or update a key in cache, the update operate when occurred that cache was expired, else get old value.
var value = cache.Get("key", () => "new value");
```

### Lifetime Cache example:

```C#

// define lifetime cache
var lifetimeCache = new AutoRefreshingCache<object>(cacheName: "lifetimeCache");
lifetimeCache.Inject("test", 123456);
int testValue = lifetimeCache.Get<int>("test");
```

### Rate Limiter example:

```C#

// define rate limiter for decide can call a method too more or not?
var rateLimiter = new RateLimiter(maxTries: 100, inPeriod: 120, cacheName: "rateLimiterCache");
bool canProc = rateLimiter.CanProceed("method name or a key");
```

### Request Limiter by IP Filter example:

```C#

// use rate limiter in Web API or MVC Controller to limit request count for all actions by IP filtering
[RequestLimiterByIpFilter] // default: maxTries: 2000, inPeriod: 3600, filterName: nameof(RequestLimiterByIpFilterAttribute)
[RequestLimiterByIpFilter(maxTries: 100, inPeriod: 120, filterName: nameof(TestController))] // customized
public class TestController : Controller
{
	public IActionResult GetTest()
	{
		// ...
	}

	.
	.
	.
}
```