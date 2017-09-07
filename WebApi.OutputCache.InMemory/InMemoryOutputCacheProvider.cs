using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using WebApi.OutputCache.Core;

namespace WebApi.OutputCache.InMemory
{
    public class InMemoryOutputCacheProvider : IApiOutputCache
    {
        private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        public virtual void RemoveStartsWith(string key)
        {
            Cache.Remove(key);
        }

        public virtual T Get<T>(string key) where T : class
        {
            return Cache.Get(key) as T;
        }

        public virtual void Remove(string key)
        {
            Cache.Remove(key);
        }

        public virtual bool Contains(string key)
        {
            return Cache.TryGetValue(key, out object result);
        }

        public virtual void Add(string key, object o, DateTimeOffset expiration, string dependsOnKey = null)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<string> AllKeys
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
