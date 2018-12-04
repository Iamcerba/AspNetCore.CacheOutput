using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace AspNetCore.CacheOutput.Redis.Extensions
{
    /// <summary>
    /// Extension methods for setting up output cache related services in an <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    public static class CacheOutputServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an StackExchange.Redis implementation of <see cref="T:AspNetCore.CacheOutput.IApiCacheOutput" /> to the
        /// <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add services to.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
        public static IServiceCollection AddRedisCacheOutput(this IServiceCollection services, string connectionString)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            services.TryAdd(ServiceDescriptor.Singleton<CacheKeyGeneratorFactory, CacheKeyGeneratorFactory>());
            services.TryAdd(ServiceDescriptor.Singleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>());
            services.TryAdd(ServiceDescriptor.Singleton<IApiCacheOutput, StackExchangeRedisCacheOutputProvider>());
            services.TryAdd(ServiceDescriptor.Singleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(connectionString)));
            services.TryAdd(ServiceDescriptor.Transient<IDatabase>(e => e.GetRequiredService<IConnectionMultiplexer>().GetDatabase(-1, null)));

            return services;
        }
    }
}
