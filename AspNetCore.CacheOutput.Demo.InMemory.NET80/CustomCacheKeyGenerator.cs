using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCore.CacheOutput.Demo.InMemory.NET80
{
    public class CustomCacheKeyGenerator : DefaultCacheKeyGenerator
    {
        public override string MakeCacheKey(
            ActionExecutingContext context,
            string mediaType,
            bool excludeQueryString = false
        )
        {
            return base.MakeCacheKey(context, mediaType, excludeQueryString);
        }
    }
}
