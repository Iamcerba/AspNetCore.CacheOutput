using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.CacheOutput
{
    public class CacheOutputMiddleware
    {
        protected RequestDelegate next;

        public CacheOutputMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            using (var stream = new MemoryStream())
            {
                Stream originalResponse = context.Response.Body;
                context.Response.Body = stream;

                await next.Invoke(context);

                stream.Seek(0, SeekOrigin.Begin);
                await stream.CopyToAsync(originalResponse);
            }
        }
    }
}
