using System;
using Microsoft.AspNetCore.Builder;

namespace WebApi.OutputCache.Extensions
{
    public static class AppBuilderExtensions
    {
        public static IApplicationBuilder UseCacheOutput(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseMiddleware<CacheOutputMiddleware>();
        }
    }
}
