using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AspNetCore.CacheOutput.InMemory.Extensions
{
    /// <summary>
    /// Extension methods for setting up output cache related services in an <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    public static class CacheOutputServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an in memory implementation of <see cref="T:AspNetCore.CacheOutput.IApiCacheOutput" /> to the
        /// <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
        public static IServiceCollection AddInMemoryCacheOutput(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAdd(ServiceDescriptor.Singleton<CacheKeyGeneratorFactory, CacheKeyGeneratorFactory>());
            services.TryAdd(ServiceDescriptor.Singleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>());
            services.TryAdd(ServiceDescriptor.Singleton<IApiCacheOutput, InMemoryCacheOutputProvider>());
            services.TryAdd(ServiceDescriptor.Transient<IMemoryCache>(e => new MemoryCache(new MemoryCacheOptions())));

            return services;
        }
    }
}
