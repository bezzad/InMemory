using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InMemory.Test
{
    internal class Program
    {
        private static void Main()
        {
            var cancelSrc = new CancellationTokenSource();

            // define auto refreshing cache
            new AutoRefreshingCache<string>(Helper.CacheName, Helper.ExpireAfter, Helper.RefreshAfter).Inject(Helper.A, Helper.A);
            new AutoRefreshingCache<string>(Helper.CacheName, Helper.ExpireAfter, Helper.RefreshAfter).Inject(Helper.B, Helper.B);
            new AutoRefreshingCache<string>(Helper.CacheName, Helper.ExpireAfter, Helper.RefreshAfter).Inject(Helper.C, Helper.C);

            // define lifetime cache
            new AutoRefreshingCache<object>(Helper.Lifetime).Inject("key", 123456);

            Helper.Run(cancelSrc.Token);

            Console.ReadLine();
            cancelSrc.Cancel(false);
        }
    }

    public static class Helper
    {
        internal const string CacheName = "test", Lifetime= "lifetimeCache", A = "a", B = "b", C = "c", RateObj = "RateObj";
        internal const int SleepTime = 100; // ms
        internal const int ExpireAfter = 4; //sec
        internal const int RefreshAfter = 4; //sec
        internal const int MaxTries = 10; // time in period time
        internal const int InPeriod = 10; // refresh period time in sec

        private static readonly RateLimiter Rate = new RateLimiter(MaxTries, InPeriod, CacheName);
        private static int _expireCounter = 1;
        private static int _counter = 1;
        private static readonly Stopwatch Timer = Stopwatch.StartNew();
        private static int? _lockedNumber;
        private static List<int> CalcTimes { get; } = new List<int>();


        public static TOut GetLifeTimeCache<TOut>(string cacheName, string key)
        {
            // define auto refreshing cache
            var lifetimeCache = new AutoRefreshingCache<object>(cacheName);
            return lifetimeCache.Get<TOut>(key);
        }


        public static string GetCache(string cacheName, string key)
        {
            var arc = new AutoRefreshingCache<string>(cacheName, ExpireAfter, RefreshAfter);
            return arc.Get(key, () => key + _expireCounter++);
        }

        public static bool CallLocalMethods(string key)
        {
            return Rate.CanProceed(key);
        }

        public static async void Run(CancellationToken cancel)
        {
            do
            {
                var startTime = Environment.TickCount;
                Console.Clear();

                Console.WriteLine($"Calling .No #{_counter++} at time: {Timer.Elapsed.TotalSeconds:##.## 'sec'}");
                Console.WriteLine("----------------------------------------------------------------------");
                Console.WriteLine($"Lifetime memory cache");
                Console.WriteLine($"key[\"key\"]: {GetLifeTimeCache<int>(Lifetime, "key")}\n");
                Console.WriteLine($"Auto refreshable memory cache after {RefreshAfter}sec for key {A}, {B} and {C}");
                Console.WriteLine($"key[{A}]: {GetCache(CacheName, A)}");
                Console.WriteLine($"key[{B}]: {GetCache(CacheName, B)}");
                Console.WriteLine($"key[{C}]: {GetCache(CacheName, C)}");

                Console.WriteLine();
                Console.WriteLine($"Test rate limiter for {MaxTries} call per {InPeriod} sec");
                Console.Write($"Can call Local Methods('{RateObj}') ? ");

                if (_lockedNumber == null || _lockedNumber < _counter)
                {
                    var can = CallLocalMethods(RateObj);
                    if (!can)
                        _lockedNumber = _counter + InPeriod * 1000 / SleepTime; // run again after _lockedTime count later 

                    var res = can ? "Yes" : "No!";
                    Console.WriteLine(res);
                }
                else
                {
                    Console.WriteLine("No!");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($" locked till .No #{_lockedNumber} at time {_lockedNumber * CalcTimes.Average() / 1000:##.## 'sec'}");
                    Console.ResetColor();
                }
                await Task.Delay(SleepTime, cancel);
                CalcTimes.Add(Environment.TickCount - startTime);
            } while (!cancel.IsCancellationRequested);
        }

        public static void Test()
        {
            // define auto refreshing cache
            var refreshableCache = new AutoRefreshingCache<string>(expireAfter: 10, refreshAfter: 8, cacheName: "shortTimeCache");

            // add key/value data to cache
            refreshableCache.Inject("key", "value");

            // get count of expired cache by key elements
            int expiredCacheCount = refreshableCache.CountExpiredElements(new[] { "key1", "key2", "key3", "key4" });

            // get or update a key in cache, the update operate when occurred that cache was expired, else get old value.
            var value = refreshableCache.Get("key", () => "new value");

            // define lifetime cache
            var lifetimeCache = new AutoRefreshingCache<object>(cacheName: "lifetimeCache");
            lifetimeCache.Inject("test", 123456);
            int testValue = lifetimeCache.Get<int>("test");


            // define rate limiter for decide can call a method too more or not?
            var rateLimiter = new RateLimiter(maxTries: 100, inPeriod: 120, cacheName: "rateLimiterCache");
            bool canProc = rateLimiter.CanProceed("method name or a key");
        }
    }
}
