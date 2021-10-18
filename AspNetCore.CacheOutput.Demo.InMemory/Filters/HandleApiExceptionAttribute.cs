using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCore.CacheOutput.Demo.InMemory.Filters
{
    public class HandleApiExceptionAttribute : ExceptionFilterAttribute
    {
        public override Task OnExceptionAsync(ExceptionContext context)
        {
            var exception = context.Exception;

            var obj = new
            {
                Status = 2,
                Good = false,
                Log = exception.Message
            };

            context.Result = new JsonResult(obj);

            context.ExceptionHandled = true;

            return Task.CompletedTask;
        }
    }
}
