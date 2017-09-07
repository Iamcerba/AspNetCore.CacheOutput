using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace WebApi.OutputCache.Redis.Extensions
{
    public static class StackExchangeRedisExtensions
    {
        public static T Get<T>(this IDatabase cache, string key)
        {
            RedisValue result = cache.StringGet(key);

            if (result.IsNull)
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(result.ToString());
        }

        public static async Task<T> GetAsync<T>(this IDatabase cache, string key)
        {
            RedisValue result = await cache.StringGetAsync(key);

            if (result.IsNull)
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(result.ToString());
        }

        public static object Get(this IDatabase cache, string key)
        {
            RedisValue result = cache.StringGet(key);

            if (result.IsNull)
            {
                return default(object);
            }

            return JsonConvert.DeserializeObject<object>(result.ToString());
        }

        public static async Task<object> GetAsync(this IDatabase cache, string key)
        {
            RedisValue result = await cache.StringGetAsync(key);

            if (result.IsNull)
            {
                return default(object);
            }

            return JsonConvert.DeserializeObject<object>(result.ToString());
        }

        public static IEnumerable<RedisKey> GetAllKeys(this ConnectionMultiplexer connectionMultiplexer)
        {
            var keys = new HashSet<RedisKey>();

            // Could have more than one instance https://github.com/StackExchange/StackExchange.Redis/blob/master/Docs/KeysScan.md

            var endPoints = connectionMultiplexer.GetEndPoints();

            foreach (EndPoint endpoint in endPoints)
            {
                var dbKeys = connectionMultiplexer.GetServer(endpoint).Keys();

                foreach (var dbKey in dbKeys)
                {
                    if (!keys.Contains(dbKey))
                    {
                        keys.Add(dbKey);
                    }
                }
            }

            return keys;
        }

        public static IEnumerable<RedisKey> SearchKeys(this ConnectionMultiplexer connectionMultiplexer, string searchPattern)
        {
            var keys = new HashSet<RedisKey>();

            // Could have more than one instance https://github.com/StackExchange/StackExchange.Redis/blob/master/Docs/KeysScan.md

            var endPoints = connectionMultiplexer.GetEndPoints();

            foreach (EndPoint endpoint in endPoints)
            {
                var dbKeys = connectionMultiplexer.GetServer(endpoint).Keys(pattern: searchPattern);

                foreach (var dbKey in dbKeys)
                {
                    if (!keys.Contains(dbKey))
                    {
                        keys.Add(dbKey);
                    }
                }
            }

            return keys;
        }

        public static bool Set(this IDatabase cache, string key, object value, TimeSpan? expiry = null)
        {
            return cache.StringSet(key, JsonConvert.SerializeObject(value), expiry);
        }

        public static Task<bool> SetAsync(this IDatabase cache, string key, object value, TimeSpan? expiry = null)
        {
            return cache.StringSetAsync(key, JsonConvert.SerializeObject(value), expiry);
        }
    }
}
