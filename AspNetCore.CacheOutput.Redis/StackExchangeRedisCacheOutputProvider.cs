using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AspNetCore.CacheOutput.Redis.Extensions;
using StackExchange.Redis;

namespace AspNetCore.CacheOutput.Redis
{
    public class StackExchangeRedisCacheOutputProvider : IApiCacheOutput
    {
        private const string WildcardCharacter = "*";

        protected readonly IDatabase redisCache;

        public StackExchangeRedisCacheOutputProvider(IDatabase redisCache)
        {
            this.redisCache = redisCache;
        }

        public async Task RemoveStartsWithAsync(string key)
        {
            if (key.Contains(WildcardCharacter))
            {
                // Partial cache invalidation using wildcards
                EndPoint[] endPoints = redisCache.Multiplexer.GetEndPoints();

                foreach (EndPoint endPoint in endPoints)
                {
                    IServer server = redisCache.Multiplexer.GetServer(endPoint);

                    IList<RedisKey> keys = server
                        .Keys(pattern: key)
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

                foreach (RedisValue memberKey in keys)
                {
                    await RemoveAsync(memberKey);
                }

                await RemoveAsync(key);
            }
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            return await redisCache.GetAsync<T>(key);
        }

        public Task RemoveAsync(string key)
        {
            return redisCache.KeyDeleteAsync(key);
        }

        public async Task<bool> ContainsAsync(string key)
        {
            if (key.Contains(WildcardCharacter))
            {
                EndPoint[] endPoints = redisCache.Multiplexer.GetEndPoints();

                foreach (EndPoint endPoint in endPoints)
                {
                    IServer server = redisCache.Multiplexer.GetServer(endPoint);

                    IList<RedisKey> keys = server
                        .Keys(pattern: key)
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

            bool primaryAdded = await redisCache.SetAsync(key, value, expiration.Subtract(DateTimeOffset.Now));

            if (dependsOnKey != null && primaryAdded)
            {
                await redisCache.SetAddAsync(dependsOnKey, key);
            }
        }
    }
}
