using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCore.CacheOutput
{
    public interface ICacheKeyGenerator
    {
        string MakeCacheKey(ActionExecutingContext context, string mediaType, bool excludeQueryString = false);

        string MakeBaseCacheKey(string controller, string action);
    }
}
