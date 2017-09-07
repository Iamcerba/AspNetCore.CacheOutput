using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using StackExchange.Redis;
using WebApi.OutputCache.Core;
using WebApi.OutputCache.Redis.Extensions;

namespace WebApi.OutputCache.Redis
{
    public class StackExchangeRedisOutputCacheProvider : IApiOutputCache
    {
        private readonly IDatabase redisCache;

        public StackExchangeRedisOutputCacheProvider(IDatabase redisCache)
        {
            this.redisCache = redisCache;
        }

        public IEnumerable<string> AllKeys
        {
            get
            {
                foreach (string key in redisCache.Multiplexer.GetAllKeys())
                {
                    yield return key;
                }
            }
        }

        public void RemoveStartsWith(string key)
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
                        Remove(memberKey);
                    }
                }
            }
            else
            {
                RedisValue[] keys = redisCache.SetMembers(key);

                foreach (var memberKey in keys)
                {
                    Remove(memberKey);
                }

                Remove(key);
            }
        }

        public T Get<T>(string key) where T : class
        {
            T result = redisCache.Get<T>(key);

            if (typeof(T) == typeof(byte[]))
            {
                // GZip decompression
                return (T)Convert.ChangeType(((byte[])(object)result).Decompress(), typeof(T));
            }

            return result;
        }

        public object Get(string key)
        {
            object result = redisCache.Get(key);

            byte[] compressedArray = result as byte[];

            if (compressedArray != null)
            {
                // GZip decompression
                return compressedArray.Decompress();
            }

            return result;
        }

        public void Remove(string key)
        {
            redisCache.KeyDelete(key);
        }

        public bool Contains(string key)
        {
            if (key.Contains("*"))
            {
                EndPoint[] endPoints = redisCache.Multiplexer.GetEndPoints();

                foreach (var endPoint in endPoints)
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

            return redisCache.KeyExists(key);
        }

        public void Add(string key, object value, DateTimeOffset expiration, string dependsOnKey = null)
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

            bool primaryAdded = redisCache.Set(key, value, expiration.Subtract(DateTimeOffset.Now));

            if (dependsOnKey != null && primaryAdded)
            {
                redisCache.SetAdd(dependsOnKey, key);
            }
        }
    }
}
