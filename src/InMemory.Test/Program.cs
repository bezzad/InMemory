using System;
using System.Diagnostics;
using System.Threading;

namespace InMemory.Test
{
    class Program
    {
        static void Main()
        {
            var cacheName = "test";
            var timer = Stopwatch.StartNew();
            var arc = new AutoRefreshingCache<string>(4, 10, cacheName);
            arc.Inject("a", "a");
            arc.Inject("b", "b");
            arc.Inject("c", "c");
            arc.Inject("d", "d");

            do
            {
                Console.WriteLine($"Calling time: {timer.Elapsed.TotalSeconds:## 'sec'}");
                Console.WriteLine($"key[a]: {Helper.GetCache(cacheName, "a")}");
                Console.WriteLine($"key[b]: {Helper.GetCache(cacheName, "b")}");
                Console.WriteLine($"key[c]: {Helper.GetCache(cacheName, "c")}");
                Console.WriteLine($"key[d]: {Helper.GetCache(cacheName, "d")}");

                Thread.Sleep(1000);
                //Console.ReadKey();
            } while (true);
        }
    }

    public static class Helper
    {
        private static int _expireCounter = 1;
        public static string GetCache(string cacheName, string key)
        {
            var arc = new AutoRefreshingCache<string>(10, 5, cacheName);
            return arc.Get(key, () => key + _expireCounter++);
        }
    }
}
