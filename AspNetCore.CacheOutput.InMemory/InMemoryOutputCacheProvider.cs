using System;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.CacheOutput.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace AspNetCore.CacheOutput.InMemory
{
    public class InMemoryOutputCacheProvider : IApiOutputCache
    {
        private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
        private const string CancellationTokenKey = ":cts";

        public Task RemoveStartsWithAsync(string key)
        {
            return RemoveAsync(key);
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            return Cache.Get(key) as T;
        }

        public async Task RemoveAsync(string key)
        {
            if (Cache.TryGetValue($"{key}{CancellationTokenKey}", out CancellationTokenSource cts))
            {
                cts.Cancel();
            }
            else
            {
                Cache.Remove(key);
            }
        }

        public async Task<bool> ContainsAsync(string key)
        {
            return Cache.TryGetValue(key, out object result);
        }

        public async Task AddAsync(string key, object value, DateTimeOffset expiration, string dependsOnKey = null)
        {
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expiration.Subtract(DateTimeOffset.Now));

            if (string.IsNullOrEmpty(dependsOnKey))
            {
                var cts = new CancellationTokenSource();

                options.AddExpirationToken(new CancellationChangeToken(cts.Token));

                Cache.Set(key, value, options);

                Cache.Set($"{key}{CancellationTokenKey}", cts, options);
            }
            else
            {
                if (Cache.TryGetValue($"{dependsOnKey}{CancellationTokenKey}", out CancellationTokenSource existingCts))
                {
                    Cache.Set(key, value, new CancellationChangeToken(existingCts.Token));
                }
            }
        }
    }
}
