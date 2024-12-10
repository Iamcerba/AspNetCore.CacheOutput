using System;
using System.Threading.Tasks;
#if NET8_0
using Jil;
#endif
using StackExchange.Redis;

namespace AspNetCore.CacheOutput.Redis.Extensions
{
    internal static class StackExchangeRedisExtensions
    {
        internal static async Task<T> GetAsync<T>(this IDatabase cache, string key)
        {
            RedisValue result = await cache.StringGetAsync(key);

            if (result.IsNull)
            {
                return default(T);
            }

#if NET8_0
            return JSON.Deserialize<T>(result);
#else
            return System.Text.Json.JsonSerializer.Deserialize<T>(result);
#endif
        }

        internal static Task<bool> SetAsync(this IDatabase cache, string key, object value, TimeSpan? expiry = null)
        {
#if NET8_0
            return cache.StringSetAsync(key, JSON.Serialize(value), expiry);
#else
            return cache.StringSetAsync(key, System.Text.Json.JsonSerializer.Serialize(value), expiry);
#endif
        }
    }
}
