using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace AspNetCore.CacheOutput.InMemory
{
    public class InMemoryCacheOutputProvider : IApiCacheOutput
    {
        private const string CancellationTokenKey = ":cts";

        protected readonly IMemoryCache cache;

        public InMemoryCacheOutputProvider(IMemoryCache cache)
        {
            this.cache = cache;
        }

        public Task RemoveStartsWithAsync(string key)
        {
            return RemoveAsync(key);
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            return cache.Get(key) as T;
        }

        public async Task RemoveAsync(string key)
        {
            if (cache.TryGetValue($"{key}{CancellationTokenKey}", out CancellationTokenSource cts))
            {
                cts.Cancel();
            }
            else
            {
                cache.Remove(key);
            }
        }

        public async Task<bool> ContainsAsync(string key)
        {
            return cache.TryGetValue(key, out object result);
        }

        public async Task AddAsync(string key, object value, DateTimeOffset expiration, string dependsOnKey = null)
        {
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expiration.Subtract(DateTimeOffset.Now));

            if (string.IsNullOrEmpty(dependsOnKey))
            {
                if (cache.TryGetValue($"{key}{CancellationTokenKey}", out CancellationTokenSource existingCts))
                {
                    options.AddExpirationToken(new CancellationChangeToken(existingCts.Token));
                }
                else
                {
                    var cts = new CancellationTokenSource();

                    options.AddExpirationToken(new CancellationChangeToken(cts.Token));

                    cache.Set($"{key}{CancellationTokenKey}", cts, options);
                }

                cache.Set(key, value, options);
            }
            else
            {
                if (cache.TryGetValue($"{dependsOnKey}{CancellationTokenKey}", out CancellationTokenSource existingCts))
                {
                    options.AddExpirationToken(new CancellationChangeToken(existingCts.Token));

                    cache.Set(key, value, options);
                }
            }
        }
    }
}
