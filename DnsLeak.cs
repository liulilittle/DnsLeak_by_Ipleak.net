//Usage:
//DnsLeak.Testing((addresses) =>
//{
//    foreach (IPAddress address in addresses)
//    {
//        Console.WriteLine(address);
//    }
//}, 5);
//DnsLeak.Testing((address) => Console.WriteLine(address));

namespace DnsToolkit
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Security;
    using System.Threading.Tasks;

    public static class DnsLeak
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Random _random = new Random();

        [SecurityCritical]
        [SecuritySafeCritical]
        private static string MakeID()
        {
            var text = ""; // https://ipleak.net/
            var possible = "abcdefghijklmnopqrstuvwxyz0123456789";
            for (var i = 0; i < 40; i++)
            {
                var j = (int)Math.Floor(_random.NextDouble() * possible.Length);
                text += possible[j];
            }
            return text;
        }

        [SecurityCritical]
        [SecuritySafeCritical]
        public static void Testing(Action<IPAddress> callback)
        {
            if (callback == null)
            {
                throw new InvalidOperationException(nameof(callback));
            }
            Task.Run(async () =>
            {
                var leakIP = await Testing();
                callback(leakIP);
            });
        }

        [SecurityCritical]
        [SecuritySafeCritical]
        public static void Testing(Action<IPAddress[]> callback, int count)
        {
            if (callback == null)
            {
                throw new InvalidOperationException(nameof(callback));
            }
            Task.Run(async () =>
            {
                var addresses = await Testing(count);
                callback(addresses);
            });
        }

        public static async Task<IPAddress[]> Testing(int count)
        {
            if (count < 1)
            {
                count = 1;
            }
            Task<IPAddress>[] tasks = new Task<IPAddress>[count];
            for (int i = 0; i < count; i++)
            {
                tasks[i] = Testing();
            }
            await Task.WhenAll(tasks);
            var addresses = new List<IPAddress>();
            var set = new HashSet<IPAddress>();
            foreach (var task in tasks)
            {
                var address = task.Result;
                if (address == null || !set.Add(address))
                {
                    continue;
                }
                addresses.Add(address);
            }
            return addresses.ToArray();
        }

        public static async Task<IPAddress> Testing()
        {
            var detectUrl = "https://" + MakeID() + ".ipleak.net/dnsdetect/";
            try
            {
                using (WebClient wc = new WebClient())
                {
                    try
                    {
                        var leakIP = await wc.DownloadStringTaskAsync(detectUrl);
                        if (string.IsNullOrEmpty(leakIP))
                        {
                            return null;
                        }
                        if (!IPAddress.TryParse(leakIP, out IPAddress leakAddress))
                        {
                            return null;
                        }
                        if (leakAddress == null)
                        {
                            return null;
                        }
                        return leakAddress;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
