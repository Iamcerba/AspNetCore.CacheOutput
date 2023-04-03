using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AspNetCore.CacheOutput.Time;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace AspNetCore.CacheOutput
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class CacheOutputAttribute : ActionFilterAttribute
    {
        private const string OriginalStreamCacheKey = "CacheOutput:OriginalStream";
        private const string CurrentRequestCacheKey = "CacheOutput:CacheKey";
        private const string CurrentRequestSkipResultExecution = "CacheOutput:SkipResultExecutionKey";
        private const string ClientTimeSpanGetterValidationMessage = "Should not be called without value set";
        private const string SharedTimeSpanGetterValidationMessage = ClientTimeSpanGetterValidationMessage;

        protected static string DefaultMediaType = "application/json; charset=utf-8";

        internal IModelQuery<DateTime, CacheTime> CacheTimeQuery;

        /// <summary>
        /// Cache enabled only for requests when Thread.CurrentPrincipal is not set.
        /// </summary>
        public bool AnonymousOnly { get; set; }

        /// <summary>
        /// Corresponds to MustRevalidate HTTP header - indicates whether the origin server requires revalidation of
        /// a cache entry on any subsequent use when the cache entry becomes stale.
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

        private int? clientTimeSpan = null;

        /// <summary>
        /// Corresponds to CacheControl MaxAge HTTP header (in seconds).
        /// </summary>
        public int ClientTimeSpan
        {
            get
            {
                if (!clientTimeSpan.HasValue)
                {
                    throw new Exception(ClientTimeSpanGetterValidationMessage);
                }

                return clientTimeSpan.Value;
            }
            set => clientTimeSpan = value;
        }

        private int? sharedTimeSpan = null;

        /// <summary>
        /// Corresponds to CacheControl Shared MaxAge HTTP header (in seconds).
        /// </summary>
        public int SharedTimeSpan
        {
            get
            {
                if (!sharedTimeSpan.HasValue)
                {
                    throw new Exception(SharedTimeSpanGetterValidationMessage);
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
        /// Corresponds to CacheControl Private HTTP header. Response can be cached by browser
        /// but not by intermediary cache.
        /// </summary>
        public bool Private { get; set; } = true;

        /// <summary>
        /// Corresponds to CacheControl Public HTTP header. The "public" response directive indicates that any cache 
        /// MAY store the response, even if the response would normally be non-cacheable or cacheable only within 
        /// a private cache.
        /// </summary>
        public bool Public { get; set; }

        /// <summary>
        /// Class used to generate caching keys
        /// </summary>
        public Type CacheKeyGenerator { get; set; }

        /// <summary>
        /// If set to something else than an empty string, this value will always be used for the Content-Type header,
        /// regardless of content negotiation.
        /// </summary>
        public string MediaType { get; set; }

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

            SwapResponseBodyToMemoryStream(context);

            IServiceProvider serviceProvider = context.HttpContext.RequestServices;
            IApiCacheOutput cache = serviceProvider.GetRequiredService(typeof(IApiCacheOutput)) as IApiCacheOutput;
            CacheKeyGeneratorFactory cacheKeyGeneratorFactory = 
                serviceProvider.GetRequiredService(typeof(CacheKeyGeneratorFactory)) as CacheKeyGeneratorFactory;
            ICacheKeyGenerator cacheKeyGenerator = 
                cacheKeyGeneratorFactory.GetCacheKeyGenerator(this.CacheKeyGenerator);

            EnsureCacheTimeQuery();

            string expectedMediaType = GetExpectedMediaType(context);

            string cacheKey = cacheKeyGenerator.MakeCacheKey(
                context,
                expectedMediaType,
                ExcludeQueryStringFromCacheKey
            );

            context.HttpContext.Items[CurrentRequestCacheKey] = cacheKey;

            if (!await cache.ContainsAsync(cacheKey))
            {
                ActionExecutedContext result = await next();

                if (result.Exception != null)
                {
                    await SwapMemoryStreamToResponseBody(context);
                }

                return;
            }

            context.HttpContext.Items[CurrentRequestSkipResultExecution] = true;

            var responseAdditionalDataJsonUtf8Bytes = 
                await cache.GetAsync<byte[]>(cacheKey + Constants.ResponseAdditionalDataKey);

            var responseAdditionalData = DeserializeResponseAdditionalDataFromJsonUtf8Bytes(
                responseAdditionalDataJsonUtf8Bytes
            );

            if (context.HttpContext.Request.Headers[HeaderNames.IfNoneMatch].Any())
            {
                string etag = responseAdditionalData?.Etag;

                if (etag != null)
                {
                    if (context.HttpContext.Request.Headers[HeaderNames.IfNoneMatch].Any(e => e == etag))
                    {
                        SetEtag(context.HttpContext.Response, etag);

                        CacheTime time = CacheTimeQuery.Execute(DateTime.Now);

                        ApplyCacheHeaders(context.HttpContext.Response, time);

                        context.HttpContext.Response.ContentLength = 0;
                        context.HttpContext.Response.StatusCode = StatusCodes.Status304NotModified;

                        return;
                    }
                }
            }

            byte[] val = await cache.GetAsync<byte[]>(cacheKey);

            if (val == null)
            {
                ActionExecutedContext result = await next();

                if (result.Exception != null)
                {
                    await SwapMemoryStreamToResponseBody(context);
                }

                return;
            }

            await context.HttpContext.Response.Body.WriteAsync(val, 0, val.Length);

            string contentType = responseAdditionalData?.ContentType ?? expectedMediaType;

            context.HttpContext.Response.Headers[HeaderNames.ContentType] = contentType;

            context.HttpContext.Response.StatusCode = responseAdditionalData?.StatusCode ?? StatusCodes.Status200OK;

            string responseEtag = responseAdditionalData?.Etag;

            if (responseEtag != null)
            {
                SetEtag(context.HttpContext.Response, responseEtag);
            }

            CacheTime cacheTime = CacheTimeQuery.Execute(DateTime.Now);

            DateTimeOffset? lastModified = responseAdditionalData?.LastModified;

            ApplyCacheHeaders(context.HttpContext.Response, cacheTime, lastModified);
        }

        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            await base.OnResultExecutionAsync(context, next);

            if (
                context.HttpContext.RequestAborted.IsCancellationRequested ||
                context.HttpContext.Response == null ||
                context.HttpContext.Items[CurrentRequestSkipResultExecution] != null ||
                !(
                    context.HttpContext.Response.StatusCode >= (int)HttpStatusCode.OK &&
                    context.HttpContext.Response.StatusCode < (int)HttpStatusCode.Ambiguous
                )
            )
            {
                await SwapMemoryStreamToResponseBody(context);

                return;
            }

            if (!IsCachingAllowed(context, AnonymousOnly))
            {
                await SwapMemoryStreamToResponseBody(context);

                return;
            }

            DateTimeOffset actionExecutionTimestamp = DateTimeOffset.Now;
            CacheTime cacheTime = CacheTimeQuery.Execute(actionExecutionTimestamp.DateTime);

            if (cacheTime.AbsoluteExpiration > actionExecutionTimestamp)
            {
                IServiceProvider serviceProvider = context.HttpContext.RequestServices;
                IApiCacheOutput cache = serviceProvider.GetRequiredService(typeof(IApiCacheOutput)) as IApiCacheOutput;
                CacheKeyGeneratorFactory cacheKeyGeneratorFactory = 
                    serviceProvider.GetRequiredService(typeof(CacheKeyGeneratorFactory)) as CacheKeyGeneratorFactory;
                ICacheKeyGenerator cacheKeyGenerator = 
                    cacheKeyGeneratorFactory.GetCacheKeyGenerator(this.CacheKeyGenerator);

                string cacheKey = context.HttpContext.Items[CurrentRequestCacheKey] as string;

                if (!string.IsNullOrWhiteSpace(cacheKey))
                {
                    if (!await cache.ContainsAsync(cacheKey))
                    {
                        SetEtag(context.HttpContext.Response, CreateEtag());

                        context.HttpContext.Response.Headers.Remove(HeaderNames.ContentLength);

                        var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
                        string controller = actionDescriptor?.ControllerTypeInfo.FullName;
                        string action = actionDescriptor?.ActionName;
                        string baseKey = cacheKeyGenerator.MakeBaseCacheKey(controller, action);
                        string contentType = context.HttpContext.Response.ContentType;
                        string etag = context.HttpContext.Response.Headers[HeaderNames.ETag];

                        var memoryStream = context.HttpContext.Response.Body as MemoryStream;

                        if (memoryStream != null)
                        {
                            byte[] content = memoryStream.ToArray();

                            await cache.AddAsync(baseKey, string.Empty, cacheTime.AbsoluteExpiration);
                            await cache.AddAsync(cacheKey, content, cacheTime.AbsoluteExpiration, baseKey);

                            var responseAdditionalData = new ResponseAdditionalData() {
                                StatusCode = context.HttpContext.Response.StatusCode,
                                ContentType = contentType,
                                Etag = etag,
                                LastModified = actionExecutionTimestamp
                            };

                            byte[] responseAdditionalDataUtf8Bytes = 
                                SerializeResponseAdditionalDataToJsonUtf8Bytes(responseAdditionalData);

                            await cache.AddAsync(
                                cacheKey + Constants.ResponseAdditionalDataKey,
                                JsonSerializer.SerializeToUtf8Bytes(responseAdditionalData),
                                cacheTime.AbsoluteExpiration,
                                baseKey
                            );
                        }
                    }
                }
            }

            ApplyCacheHeaders(context.HttpContext.Response, cacheTime, actionExecutionTimestamp);

            await SwapMemoryStreamToResponseBody(context);
        }

        protected virtual bool IsCachingAllowed(FilterContext actionContext, bool anonymousOnly)
        {
            if (anonymousOnly && (actionContext.HttpContext.User?.Identity.IsAuthenticated ?? false))
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
            CacheTimeQuery = new ShortTime(ServerTimeSpan, clientTimeSpan, sharedTimeSpan);
        }

        protected virtual string GetExpectedMediaType(ActionContext context)
        {
            if (!string.IsNullOrWhiteSpace(this.MediaType))
            {
                return this.MediaType;
            }

            IServiceProvider serviceProvider = context.HttpContext.RequestServices;
            IOptions<MvcOptions> options = 
                serviceProvider.GetService(typeof(IOptions<MvcOptions>)) as IOptions<MvcOptions>;

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

                            if (
                                outputFormatters.Any(
                                    e => e.SupportedMediaTypes.Any(
                                        t => t.ToLower() == mediaType.ToLower()
                                    )
                                )
                            )
                            {
                                return mediaType;
                            }
                        }
                    }
                }
            }

            return DefaultMediaType;
        }

        protected virtual void ApplyCacheHeaders(
            HttpResponse response,
            CacheTime cacheTime,
            DateTimeOffset? lastModified = null
        )
        {
            var cacheControl = new CacheControlHeaderValue
            {
                MaxAge = cacheTime.ClientTimeSpan,
                SharedMaxAge = cacheTime.SharedTimeSpan,
                MustRevalidate = MustRevalidate,
                Private = Private,
                Public = Public,
                NoCache = NoCache
            };

            string cacheControlHeader = cacheControl.ToString();

            if (!string.IsNullOrEmpty(cacheControlHeader)) {
                response.Headers[HeaderNames.CacheControl] = cacheControlHeader;
            }

            if (NoCache) {
                response.Headers[HeaderNames.Pragma] = "no-cache";
            }

            if (lastModified.HasValue)
            {
                response.Headers[HeaderNames.LastModified] = lastModified.Value.ToString("R");
            }
        }

        protected virtual string CreateEtag()
        {
            return Guid.NewGuid().ToString();
        }

        private static void SwapResponseBodyToMemoryStream(ActionContext context)
        {
            context.HttpContext.Items.Add(OriginalStreamCacheKey, context.HttpContext.Response.Body);
            context.HttpContext.Response.Body = new MemoryStream();
        }

        private static async Task SwapMemoryStreamToResponseBody(ActionContext context)
        {
            if (
                context.HttpContext.Response.Body is MemoryStream
                    && context.HttpContext.Items.ContainsKey(OriginalStreamCacheKey)
                    && context.HttpContext.Items[OriginalStreamCacheKey] is Stream originalStream
            )
            {
                context.HttpContext.Response.Body.Seek(0, SeekOrigin.Begin);

                await context.HttpContext.Response.Body.CopyToAsync(originalStream);

                await context.HttpContext.Response.Body.DisposeAsync();

                context.HttpContext.Response.Body = originalStream;
            }
        }

        private static void SetEtag(HttpResponse response, string etag)
        {
            if (!string.IsNullOrWhiteSpace(etag))
            {
                response.Headers[HeaderNames.ETag] = @"""" + etag.Replace("\"", string.Empty) + @"""";
            }
        }

        private byte[] SerializeResponseAdditionalDataToJsonUtf8Bytes(
            ResponseAdditionalData responseAdditionalData
        )
        {
            return JsonSerializer.SerializeToUtf8Bytes(responseAdditionalData);
        }

        private ResponseAdditionalData DeserializeResponseAdditionalDataFromJsonUtf8Bytes(
            byte[] responseAdditionalDataJsonUtf8Bytes
        )
        {
            var utf8Reader = new Utf8JsonReader(responseAdditionalDataJsonUtf8Bytes);

            return JsonSerializer.Deserialize<ResponseAdditionalData>(ref utf8Reader);
        }
    }
}
