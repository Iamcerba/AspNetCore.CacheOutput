using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.CacheOutput
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class InvalidateCacheOutputAttribute : ActionFilterAttribute
    {
        private readonly string controller;
        private readonly string methodName;
        private readonly Type cacheKeyGeneratorType;

        public InvalidateCacheOutputAttribute(string methodName, Type cacheKeyGeneratorType = default(Type))
            : this(methodName, null, cacheKeyGeneratorType)
        {
        }

        public InvalidateCacheOutputAttribute(string methodName, Type controllerType, Type cacheKeyGeneratorType = default(Type))
        {
            this.controller = controllerType != null ? controllerType.FullName : null;
            this.methodName = methodName;
            this.cacheKeyGeneratorType = cacheKeyGeneratorType;
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

            await cache.RemoveStartsWithAsync(baseCacheKey);
        }
    }
}
