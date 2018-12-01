using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace AspNetCore.CacheOutput
{
    public class DefaultCacheKeyGenerator : ICacheKeyGenerator
    {
        public virtual string MakeCacheKey(ActionExecutingContext context, string mediaType, bool excludeQueryString = false)
        {
            var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            string controller = actionDescriptor?.ControllerTypeInfo.FullName;
            string action = actionDescriptor?.ActionName;
            string key = MakeBaseCacheKey(controller, action);
            IEnumerable<string> actionParameters = context
                .ActionArguments
                .Where(x => x.Value != null)
                .Select(x => x.Key + "=" + GetValue(x.Value));

            string parameters;

            if (!excludeQueryString)
            {
                IEnumerable<string> queryStringParameters =
                    context
                        .HttpContext
                        .Request
                        .Query
                        .Where(e => e.Key.ToLower() != "callback")
                        .Select(e => e.Key + "=" + e.Value);

                IEnumerable<string> parametersCollections = actionParameters.Union(queryStringParameters);

                parameters = "-" + string.Join("&", parametersCollections);

                string callbackValue = GetJsonpCallback(context.HttpContext.Request);

                if (!string.IsNullOrWhiteSpace(callbackValue))
                {
                    string callback = "callback=" + callbackValue;

                    if (parameters.Contains("&" + callback))
                    {
                        parameters = parameters.Replace("&" + callback, string.Empty);
                    }

                    if (parameters.Contains(callback + "&"))
                    {
                        parameters = parameters.Replace(callback + "&", string.Empty);
                    }

                    if (parameters.Contains("-" + callback))
                    {
                        parameters = parameters.Replace("-" + callback, string.Empty);
                    }

                    if (parameters.EndsWith("&"))
                    {
                        parameters = parameters.TrimEnd('&');
                    }
                }
            }
            else
            {
                parameters = "-" + string.Join("&", actionParameters);
            }

            if (parameters == "-")
            {
                parameters = string.Empty;
            }

            return $"{key}{parameters}:{mediaType}";
        }

        public virtual string MakeBaseCacheKey(string controller, string action)
        {
            return $"{controller.ToLower()}-{action.ToLower()}";
        }

        private string GetJsonpCallback(HttpRequest request)
        {
            string callback = string.Empty;

            if (request.Method == HttpMethod.Get.ToString())
            {
                IQueryCollection query = request.Query;

                if (query != null)
                {
                    KeyValuePair<string, StringValues> queryVal = query.FirstOrDefault(x => x.Key.ToLower() == "callback");

                    if (!queryVal.Equals(default(KeyValuePair<string, StringValues>)))
                    {
                        callback = queryVal.Value.FirstOrDefault();
                    }
                }
            }

            return callback;
        }

        private string GetValue(object val)
        {
            if (val is IEnumerable && !(val is string))
            {
                string concatValue = string.Empty;
                var paramArray = val as IEnumerable;

                return paramArray.Cast<object>().Aggregate(concatValue, (current, paramValue) => current + (paramValue + ";"));
            }

            return val.ToString();
        }
    }
}
