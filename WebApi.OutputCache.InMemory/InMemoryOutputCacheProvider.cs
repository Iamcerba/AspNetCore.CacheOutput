using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using WebApi.OutputCache.Core;

namespace WebApi.OutputCache.InMemory
{
    public class InMemoryOutputCacheProvider : IApiOutputCache
    {
        private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        public async Task RemoveStartsWithAsync(string key)
        {
            Cache.Remove(key);
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            return Cache.Get(key) as T;
        }

        public async Task RemoveAsync(string key)
        {
            Cache.Remove(key);
        }

        public async Task<bool> ContainsAsync(string key)
        {
            return Cache.TryGetValue(key, out object result);
        }

        public Task AddAsync(string key, object o, DateTimeOffset expiration, string dependsOnKey = null)
        {
            throw new NotImplementedException();
        }
    }
}
