using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.CacheOutput.Redis
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class InvalidateCacheOutputAttribute : ActionFilterAttribute
    {
        private readonly string controller;
        private readonly string methodName;
        private readonly Type cacheKeyGeneratorType;
        private readonly string[] actionParameters;

        public InvalidateCacheOutputAttribute(string methodName, Type cacheKeyGeneratorType = default(Type))
        {
            if (cacheKeyGeneratorType != null && !typeof(ICacheKeyGenerator).IsAssignableFrom(cacheKeyGeneratorType))
            {
                throw new ArgumentException(nameof(cacheKeyGeneratorType));
            }

            this.controller = null;
            this.methodName = methodName;
            this.cacheKeyGeneratorType = cacheKeyGeneratorType;
        }

        public InvalidateCacheOutputAttribute(
            string methodName,
            Type controllerType,
            Type cacheKeyGeneratorType = default(Type),
            params string[] actionParameters
        )
        {
            if (controllerType != null && !typeof(ControllerBase).IsAssignableFrom(controllerType))
            {
                throw new ArgumentException(nameof(controllerType));
            }

            if (cacheKeyGeneratorType != null && !typeof(ICacheKeyGenerator).IsAssignableFrom(cacheKeyGeneratorType))
            {
                throw new ArgumentException(nameof(cacheKeyGeneratorType));
            }

            this.controller = controllerType != null ? controllerType.FullName : null;
            this.methodName = methodName;
            this.cacheKeyGeneratorType = cacheKeyGeneratorType;
            this.actionParameters = actionParameters;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await base.OnActionExecutionAsync(context, next);

            if (
                context.HttpContext.Response != null &&
                !(
                    context.HttpContext.Response.StatusCode >= (int)HttpStatusCode.OK &&
                    context.HttpContext.Response.StatusCode < (int)HttpStatusCode.Ambiguous
                )
            )
            {
                return;
            }

            IServiceProvider serviceProvider = context.HttpContext.RequestServices;
            IApiCacheOutput cache = serviceProvider.GetRequiredService(typeof(IApiCacheOutput)) as IApiCacheOutput;
            CacheKeyGeneratorFactory cacheKeyGeneratorFactory = serviceProvider.GetRequiredService(typeof(CacheKeyGeneratorFactory)) as CacheKeyGeneratorFactory;
            ICacheKeyGenerator cacheKeyGenerator = cacheKeyGeneratorFactory.GetCacheKeyGenerator(cacheKeyGeneratorType);

            string controllerName = this.controller ??
                (context.ActionDescriptor as ControllerActionDescriptor)?.ControllerTypeInfo.FullName;

            string baseCacheKey = cacheKeyGenerator.MakeBaseCacheKey(controllerName, this.methodName);

            string key = IncludeActionParameters(context, baseCacheKey, actionParameters);

            await cache.RemoveStartsWithAsync(key);
        }

        private string IncludeActionParameters(
            ActionExecutingContext actionContext,
            string baseCacheKey,
            string[] additionalActionParameters
        )
        {
            if (!additionalActionParameters.Any())
            {
                return $"{baseCacheKey}";
            }

            IEnumerable<string> actionContextParameters = actionContext
                .ActionArguments
                .Where(x => x.Value != null && additionalActionParameters.Contains(x.Key))
                .Select(x => x.Key + "=" + GetValue(x.Value));

            return $"{baseCacheKey}-{string.Join("-", actionContextParameters)}*";
        }

        private string GetValue(object val)
        {
            if (val is IEnumerable && !(val is string))
            {
                string concatValue = string.Empty;
                IEnumerable paramArray = (IEnumerable)val;

                return paramArray.Cast<object>().Aggregate(concatValue, (current, paramValue) => current + (paramValue + ";"));
            }

            return val.ToString();
        }
    }
}
