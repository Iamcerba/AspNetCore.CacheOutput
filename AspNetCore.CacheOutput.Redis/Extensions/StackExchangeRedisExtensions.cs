using System;
using System.Threading.Tasks;
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

            return System.Text.Json.JsonSerializer.Deserialize<T>(result);
        }

        internal static Task<bool> SetAsync(this IDatabase cache, string key, object value, TimeSpan? expiry = null)
        {
            return cache.StringSetAsync(key, System.Text.Json.JsonSerializer.Serialize(value), expiry);
        }
    }
}
