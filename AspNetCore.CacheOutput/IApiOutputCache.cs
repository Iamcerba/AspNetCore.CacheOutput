using System;
using System.Threading.Tasks;

namespace AspNetCore.CacheOutput
{
    public interface IApiOutputCache
    {
        Task RemoveStartsWithAsync(string key);

        Task<T> GetAsync<T>(string key) where T : class;

        Task RemoveAsync(string key);

        Task<bool> ContainsAsync(string key);

        Task AddAsync(string key, object value, DateTimeOffset expiration, string dependsOnKey = null);
    }
}
