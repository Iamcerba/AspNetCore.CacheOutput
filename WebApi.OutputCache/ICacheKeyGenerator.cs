using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApi.OutputCache
{
    public interface ICacheKeyGenerator
    {
        string MakeCacheKey(ActionExecutingContext context, string mediaType, bool excludeQueryString = false);

        string MakeBaseCachekey(string controller, string action);
    }
}
