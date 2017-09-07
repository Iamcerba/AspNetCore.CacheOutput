using System;
using System.Net;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApi.OutputCache.Core;

namespace WebApi.OutputCache
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class InvalidateCacheOutputAttribute : ActionFilterAttribute
    {
        private string controllerName;
        private readonly string methodName;

        public InvalidateCacheOutputAttribute(string methodName) : this(methodName, null)
        {
        }

        public InvalidateCacheOutputAttribute(string methodName, string controllerName)
        {
            this.methodName = methodName;
            this.controllerName = controllerName;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null ||
                context.HttpContext.Response != null &&
                !(
                    context.HttpContext.Response.StatusCode >= (int)HttpStatusCode.OK && 
                    context.HttpContext.Response.StatusCode < (int)HttpStatusCode.Ambiguous
                )
            )
            {
                return;
            }

            this.controllerName = this.controllerName ?? 
                (context.ActionDescriptor as ControllerActionDescriptor)?.ControllerTypeInfo.FullName;

            IServiceProvider serviceProvider = context.HttpContext.RequestServices;
            IApiOutputCache cache = serviceProvider.GetService(typeof(IApiOutputCache)) as IApiOutputCache;
            ICacheKeyGenerator cacheKeyGenerator = serviceProvider.GetService(typeof(ICacheKeyGenerator)) as ICacheKeyGenerator;

            if (cache != null && cacheKeyGenerator != null)
            {
                string baseCachekey = cacheKeyGenerator.MakeBaseCachekey(this.controllerName, this.methodName);

                cache.RemoveStartsWith(baseCachekey);
            }
        }
    }
}
