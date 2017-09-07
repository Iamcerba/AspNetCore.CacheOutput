using System;
using System.Collections.Generic;

namespace WebApi.OutputCache.Core
{
    public interface IApiOutputCache
    {
        void RemoveStartsWith(string key);

        T Get<T>(string key) where T : class;

        void Remove(string key);

        bool Contains(string key);

        void Add(string key, object o, DateTimeOffset expiration, string dependsOnKey = null);

        IEnumerable<string> AllKeys { get; }
    }
}
