using System;
using System.Diagnostics;
using System.Threading;

namespace InMemory.Test
{
    class Program
    {
        static void Main()
        {
            string cacheName = "test", a = "a", b = "b", c = "c", d = "d";
            var timer = Stopwatch.StartNew();
            var counter = 1;
            int? lockedTime = null;
            var sleepTime = 100; // ms
            var upgradeTime = 1 * 1000; // ms

            var arc = new AutoRefreshingCache<string>(4, 10, cacheName);
            arc.Inject("a", "a");
            arc.Inject("b", "b");
            arc.Inject("c", "c");
            arc.Inject("d", "d");

            do
            {
                Console.WriteLine($"Calling .No #{counter++} at time: {timer.Elapsed.TotalSeconds:##.## 'sec'}");
                Console.WriteLine("Testing in auto refreshable memory cache for key a, b, c and d");
                Console.WriteLine($"key[{a}]: {Helper.GetCache(cacheName, a)}");
                Console.WriteLine($"key[{b}]: {Helper.GetCache(cacheName, b)}");
                Console.WriteLine($"key[{c}]: {Helper.GetCache(cacheName, c)}");
                Console.WriteLine($"key[{d}]: {Helper.GetCache(cacheName, d)}");
                Console.WriteLine();
                Console.WriteLine("Test rate limiter for 5 call per 1 sec");
                Console.WriteLine("Can call Local Methods('{a}') ?");

                if (lockedTime == null || lockedTime < counter)
                {
                    var can = Helper.CallLocalMethods(a);
                    if (!can)
                        lockedTime = counter + upgradeTime / sleepTime; // run again after 20sec later 

                    var res = can ? "Yes" : "No!";
                    Console.WriteLine(res);
                }
                else
                {
                    Console.WriteLine("No!");

                }
                Console.WriteLine("-------------------------------------------------------------------");
                Thread.Sleep(sleepTime);
            } while (true);
        }
    }

    public static class Helper
    {
        private static readonly RateLimiter Rate = new RateLimiter(5, 1, "rateCache"); // maximum 5 call per 1 sec rate limitation
        private static int _expireCounter = 1;

        public static string GetCache(string cacheName, string key)
        {
            var arc = new AutoRefreshingCache<string>(10, 5, cacheName);
            return arc.Get(key, () => key + _expireCounter++);
        }


        public static bool CallLocalMethods(string key)
        {
            return Rate.CanProceed(key);
        }
    }
}
