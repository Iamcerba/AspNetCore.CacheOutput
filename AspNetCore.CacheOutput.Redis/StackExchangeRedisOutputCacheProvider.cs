using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AspNetCore.CacheOutput.Redis.Extensions;
using StackExchange.Redis;

namespace AspNetCore.CacheOutput.Redis
{
    public class StackExchangeRedisOutputCacheProvider : IApiOutputCache
    {
        private readonly IDatabase redisCache;

        public StackExchangeRedisOutputCacheProvider(IDatabase redisCache)
        {
            this.redisCache = redisCache;
        }

        public async Task RemoveStartsWithAsync(string key)
        {
            if (key.Contains("*"))
            {
                // Partial cache invalidation using wildcards
                EndPoint[] endPoints = redisCache.Multiplexer.GetEndPoints();

                foreach (EndPoint endPoint in endPoints)
                {
                    IServer server = redisCache.Multiplexer.GetServer(endPoint);

                    IList<RedisKey> keys = server
                        .Keys(pattern: $"{key}")
                        .ToList();

                    foreach (RedisKey memberKey in keys)
                    {
                        await RemoveAsync(memberKey);
                    }
                }
            }
            else
            {
                RedisValue[] keys = await redisCache.SetMembersAsync(key);

                foreach (var memberKey in keys)
                {
                    await RemoveAsync(memberKey);
                }

                await RemoveAsync(key);
            }
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            T result = await redisCache.GetAsync<T>(key);

            if (typeof(T) == typeof(byte[]))
            {
                // GZip decompression
                return (T)Convert.ChangeType(((byte[])(object)result).Decompress(), typeof(T));
            }

            return result;
        }

        public Task RemoveAsync(string key)
        {
            return redisCache.KeyDeleteAsync(key);
        }

        public async Task<bool> ContainsAsync(string key)
        {
            if (key.Contains("*"))
            {
                EndPoint[] endPoints = redisCache.Multiplexer.GetEndPoints();

                foreach (EndPoint endPoint in endPoints)
                {
                    IServer server = redisCache.Multiplexer.GetServer(endPoint);

                    IList<RedisKey> keys = server
                        .Keys(pattern: $"{key}")
                        .ToList();

                    if (keys.Any())
                    {
                        return true;
                    }
                }

                return false;
            }

            return await redisCache.KeyExistsAsync(key);
        }

        public async Task AddAsync(string key, object value, DateTimeOffset expiration, string dependsOnKey = null)
        {
            // Lets not store the base type (will be dependsOnKey later) since we want to use it as a set!
            if (Equals(value, string.Empty))
            {
                return;
            }

            byte[] byteArray = value as byte[];

            if (byteArray != null)
            {
                // GZip compression 
                value = byteArray.Compress();
            }

            bool primaryAdded = await redisCache.SetAsync(key, value, expiration.Subtract(DateTimeOffset.Now));

            if (dependsOnKey != null && primaryAdded)
            {
                await redisCache.SetAddAsync(dependsOnKey, key);
            }
        }
    }
}
