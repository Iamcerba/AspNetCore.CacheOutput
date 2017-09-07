using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApi.OutputCache
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class IgnoreCacheOutputAttribute : ActionFilterAttribute
    {
    }
}
