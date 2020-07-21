using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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

        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            await base.OnResultExecutionAsync(context, next);

#if NETCOREAPP2_0 || NETCOREAPP2_1
            var isCacheable = IsCacheable(context.HttpContext.Response?.StatusCode);
#else
            var result = context.Result as IStatusCodeActionResult;
            var isCacheable = result == null ? 
                IsCacheable(context.HttpContext.Response?.StatusCode) :
                IsCacheable(result.StatusCode);
#endif
            if (!isCacheable)
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

        private bool IsCacheable(int? statusCode)
        {
            return statusCode == null ||
                   (statusCode >= (int)HttpStatusCode.OK &&
                    statusCode < (int)HttpStatusCode.Ambiguous);
        }
    }
}
