using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCore.CacheOutput
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class IgnoreCacheOutputAttribute : ActionFilterAttribute
    {
    }
}
