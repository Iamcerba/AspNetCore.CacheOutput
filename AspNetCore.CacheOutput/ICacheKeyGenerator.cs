using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCore.CacheOutput
{
    public interface ICacheKeyGenerator
    {
        string MakeCacheKey(ActionExecutingContext context, string mediaType, bool excludeQueryString = false);

        string MakeBaseCachekey(string controller, string action);
    }
}
