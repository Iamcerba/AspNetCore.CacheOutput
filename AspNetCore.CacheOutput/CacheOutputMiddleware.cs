using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.CacheOutput
{
    public class CacheOutputMiddleware
    {
        protected RequestDelegate Next;

        public CacheOutputMiddleware(RequestDelegate next)
        {
            Next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            await Next.Invoke(context);
        }
    }
}
