using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCore.CacheOutput
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class InvalidateCacheOutputAttribute : ActionFilterAttribute
    {
        private readonly string controller;
        private readonly string methodName;

        public InvalidateCacheOutputAttribute(string methodName) : this(methodName, null)
        {
        }

        public InvalidateCacheOutputAttribute(string methodName, Type controllerType)
        {
            this.controller = controllerType != null ? controllerType.FullName : null;
            this.methodName = methodName;
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
            IApiOutputCache cache = serviceProvider.GetService(typeof(IApiOutputCache)) as IApiOutputCache;
            ICacheKeyGenerator cacheKeyGenerator = serviceProvider.GetService(typeof(ICacheKeyGenerator)) as ICacheKeyGenerator;

            if (cache != null && cacheKeyGenerator != null)
            {
                string controllerName = this.controller ?? 
                    (context.ActionDescriptor as ControllerActionDescriptor)?.ControllerTypeInfo.FullName;

                string baseCachekey = cacheKeyGenerator.MakeBaseCacheKey(controllerName, this.methodName);

                await cache.RemoveStartsWithAsync(baseCachekey);
            }
        }
    }
}
