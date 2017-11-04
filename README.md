# AspNetCore.CacheOutput
ASP.NET Core port of Strathweb.CacheOutput library (https://github.com/filipw/Strathweb.CacheOutput)

Initial configuration:

1. In Startup.ConfigureServices(IServiceCollection services) method add:

services.AddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();

services.AddSingleton<IApiOutputCache, InMemoryOutputCacheProvider>();

2. In Startup.Configure(IApplicationBuilder app, IHostingEnvironment env) method add:

app.UseCacheOutput();

3. Add cache filters, for example: 

[CacheOutput(ClientTimeSpan = 0, ServerTimeSpan = 3600, MustRevalidate = true, ExcludeQueryStringFromCacheKey = false)]