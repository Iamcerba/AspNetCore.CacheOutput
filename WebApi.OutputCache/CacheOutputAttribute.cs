using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using WebApi.OutputCache.Core;
using WebApi.OutputCache.Core.Time;

namespace WebApi.OutputCache
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class CacheOutputAttribute : ActionFilterAttribute
    {
        private const string CurrentRequestCacheKey = "CacheOutput:CacheKey";
        private const string CurrentRequestSkipResultExecution = "CacheOutput:SkipResultExecutionKey";
        protected static string DefaultMediaType = "application/json; charset=utf-8";
        internal IModelQuery<DateTime, CacheTime> CacheTimeQuery;
        private int? sharedTimeSpan = null;

        /// <summary>
        /// Cache enabled only for requests when Thread.CurrentPrincipal is not set.
        /// </summary>
        public bool AnonymousOnly { get; set; }

        /// <summary>
        /// Corresponds to MustRevalidate HTTP header - indicates whether the origin server requires revalidation of a cache entry on any subsequent use when the cache entry becomes stale.
        /// </summary>
        public bool MustRevalidate { get; set; }

        /// <summary>
        /// Do not vary cache by query string values.
        /// </summary>
        public bool ExcludeQueryStringFromCacheKey { get; set; }

        /// <summary>
        /// How long response should be cached on the server side (in seconds).
        /// </summary>
        public int ServerTimeSpan { get; set; }

        /// <summary>
        /// Corresponds to CacheControl MaxAge HTTP header (in seconds).
        /// </summary>
        public int ClientTimeSpan { get; set; }

        /// <summary>
        /// Corresponds to CacheControl Shared MaxAge HTTP header (in seconds).
        /// </summary>
        public int SharedTimeSpan
        {
            get
            {
                if (!sharedTimeSpan.HasValue)
                {
                    throw new Exception("Should not be called without value set");
                }

                return sharedTimeSpan.Value;
            }
            set => sharedTimeSpan = value;
        }

        /// <summary>
        /// Corresponds to CacheControl NoCache HTTP header.
        /// </summary>
        public bool NoCache { get; set; }

        /// <summary>
        /// Corresponds to CacheControl Private HTTP header. Response can be cached by browser but not by intermediary cache.
        /// </summary>
        public bool Private { get; set; }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
                
            if (context.Result != null)
            {
                return;
            }

            if (!IsCachingAllowed(context, AnonymousOnly))
            {
                await next();

                return;
            }

            IServiceProvider serviceProvider = context.HttpContext.RequestServices;
            IApiOutputCache cache = serviceProvider.GetService(typeof(IApiOutputCache)) as IApiOutputCache;
            ICacheKeyGenerator cacheKeyGenerator = serviceProvider.GetService(typeof(ICacheKeyGenerator)) as ICacheKeyGenerator;

            if (cache != null && cacheKeyGenerator != null)
            {
                EnsureCacheTimeQuery();

                string expectedMediaType = GetExpectedMediaType(context);

                string cachekey = cacheKeyGenerator.MakeCacheKey(context, expectedMediaType, ExcludeQueryStringFromCacheKey);

                context.HttpContext.Items[CurrentRequestCacheKey] = cachekey;

                if (!await cache.ContainsAsync(cachekey))
                {
                    await next();

                    return;
                }

                context.HttpContext.Items[CurrentRequestSkipResultExecution] = true;

                if (context.HttpContext.Request.Headers[HeaderNames.IfNoneMatch].Any())
                {
                    string etag = await cache.GetAsync<string>(cachekey + Constants.EtagKey);

                    if (etag != null)
                    {
                        if (context.HttpContext.Request.Headers[HeaderNames.IfNoneMatch].Any(e => e == etag))
                        {
                            CacheTime time = CacheTimeQuery.Execute(DateTime.Now);

                            context.Result = new StatusCodeResult((int)HttpStatusCode.NotModified);

                            ApplyCacheHeaders(context.HttpContext.Response, time);

                            return;
                        }
                    }
                }

                byte[] val = await cache.GetAsync<byte[]>(cachekey);

                if (val == null)
                {
                    await next();

                    return;
                }

                string responseEtag = await cache.GetAsync<string>(cachekey + Constants.EtagKey);

                if (responseEtag != null)
                {
                    SetEtag(context.HttpContext.Response, responseEtag);
                }

                string contentType = await cache.GetAsync<string>(cachekey + Constants.ContentTypeKey) ?? expectedMediaType;

                context.Result = new ContentResult()
                {
                    Content = Encoding.UTF8.GetString(val),
                    ContentType = contentType,
                    StatusCode = (int)HttpStatusCode.OK
                };

                CacheTime cacheTime = CacheTimeQuery.Execute(DateTime.Now);

                ApplyCacheHeaders(context.HttpContext.Response, cacheTime);
            }
            else
            {
                await next();
            }
        }

        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            await base.OnResultExecutionAsync(context, next);

            if (
                context.HttpContext.Response == null ||
                context.HttpContext.Items[CurrentRequestSkipResultExecution] != null ||
                !(
                    context.HttpContext.Response.StatusCode >= (int)HttpStatusCode.OK &&
                    context.HttpContext.Response.StatusCode < (int)HttpStatusCode.Ambiguous
                )
            )
            {
                return;
            }

            if (!IsCachingAllowed(context, AnonymousOnly))
            {
                return;
            }

            CacheTime cacheTime = CacheTimeQuery.Execute(DateTime.Now);

            if (cacheTime.AbsoluteExpiration > DateTime.Now)
            {
                IServiceProvider serviceProvider = context.HttpContext.RequestServices;
                IApiOutputCache cache = serviceProvider.GetService(typeof(IApiOutputCache)) as IApiOutputCache;
                ICacheKeyGenerator cacheKeyGenerator = serviceProvider.GetService(typeof(ICacheKeyGenerator)) as ICacheKeyGenerator;

                if (cache != null && cacheKeyGenerator != null)
                {
                    string cachekey = context.HttpContext.Items[CurrentRequestCacheKey] as string;

                    if (!string.IsNullOrWhiteSpace(cachekey) && !await cache.ContainsAsync(cachekey))
                    {
                        SetEtag(context.HttpContext.Response, CreateEtag());

                        context.HttpContext.Response.Headers.Remove(HeaderNames.ContentLength);

                        var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
                        string controller = actionDescriptor?.ControllerTypeInfo.FullName;
                        string action = actionDescriptor?.ActionName;
                        string baseKey = cacheKeyGenerator.MakeBaseCachekey(controller, action);
                        string contentType = context.HttpContext.Response.ContentType;
                        string etag = context.HttpContext.Response.Headers[HeaderNames.ETag];

                        context.HttpContext.Response.Body.Seek(0, SeekOrigin.Begin);

                        using (var streamReader = new StreamReader(context.HttpContext.Response.Body, Encoding.UTF8, true, 512, true))
                        {
                            string responseBodyContent = await streamReader.ReadToEndAsync();
                            byte[] responseBodyContentAsBytes = Encoding.UTF8.GetBytes(responseBodyContent);

                            await cache.AddAsync(baseKey, string.Empty, cacheTime.AbsoluteExpiration);
                            await cache.AddAsync(cachekey, responseBodyContentAsBytes, cacheTime.AbsoluteExpiration, baseKey);
                        }

                        await cache.AddAsync(
                            cachekey + Constants.ContentTypeKey,
                            contentType,
                            cacheTime.AbsoluteExpiration, baseKey
                        );

                        await cache.AddAsync(
                            cachekey + Constants.EtagKey,
                            etag,
                            cacheTime.AbsoluteExpiration, baseKey
                        );
                    }
                }
            }

            ApplyCacheHeaders(context.HttpContext.Response, cacheTime);
        }

        protected virtual bool IsCachingAllowed(FilterContext actionContext, bool anonymousOnly)
        {
            if (anonymousOnly && Thread.CurrentPrincipal.Identity.IsAuthenticated)
            {
                return false;
            }

            if (actionContext.Filters.Any(e => e is IgnoreCacheOutputAttribute))
            {
                return false;
            }

            return actionContext.HttpContext.Request.Method == HttpMethod.Get.ToString();
        }

        protected virtual void EnsureCacheTimeQuery()
        {
            if (CacheTimeQuery == null)
            {
                ResetCacheTimeQuery();
            }
        }

        protected void ResetCacheTimeQuery()
        {
            CacheTimeQuery = new ShortTime(ServerTimeSpan, ClientTimeSpan, sharedTimeSpan);
        }

        protected virtual string GetExpectedMediaType(ActionContext context)
        {
            IServiceProvider serviceProvider = context.HttpContext.RequestServices;
            IOptions<MvcOptions> options = serviceProvider.GetService(typeof(IOptions<MvcOptions>)) as IOptions<MvcOptions>;

            if (options != null)
            {
                if (context.HttpContext.Request.Headers[HeaderNames.Accept].Any())
                {
                    string acceptHeader = context.HttpContext.Request.Headers[HeaderNames.Accept].FirstOrDefault();

                    if (!string.IsNullOrEmpty(acceptHeader))
                    {
                        string mediaType = acceptHeader.Split(',').FirstOrDefault();

                        if (!string.IsNullOrEmpty(mediaType))
                        {
                            IList<OutputFormatter> outputFormatters = options
                                .Value
                                .OutputFormatters
                                .Where(e => e is OutputFormatter)
                                .Cast<OutputFormatter>()
                                .ToList();

                            if (outputFormatters.Any(e => e.SupportedMediaTypes.Any(t => t.ToLower() == mediaType.ToLower())))
                            {
                                return mediaType;
                            }
                        }
                    }
                }
            }
            
            return DefaultMediaType;
        }

        protected virtual void ApplyCacheHeaders(HttpResponse response, CacheTime cacheTime)
        {
            if (cacheTime.ClientTimeSpan > TimeSpan.Zero || MustRevalidate || Private)
            {
                response.Headers[HeaderNames.CacheControl] =
                    string.Format(
                        "private,max-age={0}{1}{2}",
                        cacheTime.ClientTimeSpan.TotalSeconds,
                        cacheTime.SharedTimeSpan != null ? $",s-maxage={cacheTime.SharedTimeSpan.Value.TotalSeconds}" : string.Empty,
                        MustRevalidate ? ",must-revalidate" : string.Empty
                    );
            }
            else if (NoCache)
            {
                response.Headers[HeaderNames.CacheControl] = "no-cache";
                response.Headers[HeaderNames.Pragma] = "no-cache";
            }
        }

        protected virtual string CreateEtag()
        {
            return Guid.NewGuid().ToString();
        }

        private static void SetEtag(HttpResponse response, string etag)
        {
            if (etag != null)
            {
                response.Headers[HeaderNames.ETag] = @"""" + etag.Replace("\"", string.Empty) + @"""";
            }
        }
    }
}
