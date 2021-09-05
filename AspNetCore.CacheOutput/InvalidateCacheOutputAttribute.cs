using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
            Type cacheKeyGeneratorType = default(Type)
        )
        {
            if (controllerType != null && !controllerType.IsAssignableFrom(typeof(ControllerBase)))
            {
                throw new ArgumentException(nameof(controllerType));
            }

            if (cacheKeyGeneratorType != null && !cacheKeyGeneratorType.IsAssignableFrom(typeof(ICacheKeyGenerator)))
            {
                throw new ArgumentException(nameof(cacheKeyGeneratorType));
            }

            this.controller = controllerType != null ? controllerType.FullName : null;
            this.methodName = methodName;
            this.cacheKeyGeneratorType = cacheKeyGeneratorType;
        }

        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            await base.OnResultExecutionAsync(context, next);

            bool isCacheable = context.Result is StatusCodeResult actionResult
                ? IsCacheableStatusCode(actionResult.StatusCode)
                : IsCacheableStatusCode(context.HttpContext.Response?.StatusCode);

            if (!isCacheable)
            {
                return;
            }

            IServiceProvider serviceProvider = context.HttpContext.RequestServices;
            IApiCacheOutput cache = serviceProvider.GetRequiredService(typeof(IApiCacheOutput)) as IApiCacheOutput;
            CacheKeyGeneratorFactory cacheKeyGeneratorFactory =
                serviceProvider.GetRequiredService(typeof(CacheKeyGeneratorFactory)) as CacheKeyGeneratorFactory;
            ICacheKeyGenerator cacheKeyGenerator =
                cacheKeyGeneratorFactory.GetCacheKeyGenerator(cacheKeyGeneratorType);

            string controllerName = this.controller ??
                (context.ActionDescriptor as ControllerActionDescriptor)?.ControllerTypeInfo.FullName;

            string baseCacheKey = cacheKeyGenerator.MakeBaseCacheKey(controllerName, this.methodName);

            await cache.RemoveStartsWithAsync(baseCacheKey);
        }

        private bool IsCacheableStatusCode(int? statusCode)
        {
            return statusCode == null ||
                (statusCode >= (int)HttpStatusCode.OK && statusCode < (int)HttpStatusCode.Ambiguous);
        }
    }
}
