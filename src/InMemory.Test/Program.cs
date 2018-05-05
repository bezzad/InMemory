using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;

namespace InMemory.Test
{
    internal class Program
    {
        private static void Main()
        {
            var cancelSrc = new CancellationTokenSource();

            new AutoRefreshingCache<string>(Helper.ExpireAfter, Helper.RefreshAfter, Helper.CacheName).Inject(Helper.A, Helper.A);
            new AutoRefreshingCache<string>(Helper.ExpireAfter, Helper.RefreshAfter, Helper.CacheName).Inject(Helper.B, Helper.B);
            new AutoRefreshingCache<string>(Helper.ExpireAfter, Helper.RefreshAfter, Helper.CacheName).Inject(Helper.C, Helper.C);

            Helper.Run(cancelSrc.Token);

            Console.ReadLine();
            cancelSrc.Cancel(false);
        }
    }

    public static class Helper
    {
        internal const string CacheName = "test", A = "a", B = "b", C = "c", RateObj = "RateObj";
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

        public static string GetCache(string cacheName, string key)
        {
            var arc = new AutoRefreshingCache<string>(ExpireAfter, RefreshAfter, cacheName);
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
                    Console.WriteLine($" locked till .No #{_lockedNumber} at time {_lockedNumber *  CalcTimes.Average() / 1000:##.## 'sec'}");
                    Console.ResetColor();
                }
                await Task.Delay(SleepTime, cancel);
                CalcTimes.Add(Environment.TickCount - startTime);
            } while (!cancel.IsCancellationRequested);
        }

        public static void Test()
        {
            // define auto refreshing cache
            var cache = new AutoRefreshingCache<string>(expireAfter: 10, refreshAfter: 8, cacheName: "shortTimeCache");

            // add key/value data to cache
            cache.Inject("key", "value");

            // get count of expired cache by key elements
            int expiredCacheCount = cache.CountExpiredElements(new[] { "key1", "key2", "key3", "key4" });

            // get or update a key in cache, the update operate when occurred that cache was expired, else get old value.
            var value = cache.Get("key", () => "new value");


            // define rate limiter for decide can call a method too more or not?
            var rateLimiter = new RateLimiter(maxTries: 100, inPeriod: 120, cacheName: "rateLimiterCache");
            bool canProc = rateLimiter.CanProceed("method name or a key");
        }
    }
}
