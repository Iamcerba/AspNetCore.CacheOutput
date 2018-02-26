# AspNetCore.CacheOutput

ASP.NET Core port of Strathweb.CacheOutput library (https://github.com/filipw/Strathweb.CacheOutput)

### Initial configuration:

1. Install core package: **Install-Package AspNetCore.CacheOutput**

2. Depending on which cache provider you decided to use install any of the following packages:

   * **Install-Package AspNetCore.CacheOutput.InMemory**

   * **Install-Package AspNetCore.CacheOutput.Redis**

3. In "Startup" class "ConfigureServices" method:

   * Register cache key generator:
   
     ```csharp
     services.AddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();
     ```
   
   * Depending on previosly installed package register cache key provider:
   
     ```csharp
     services.AddSingleton<IApiOutputCache, InMemoryOutputCacheProvider>();
     ```
   
     OR
   
     ```csharp
     services.AddSingleton<IApiOutputCache, StackExchangeRedisOutputCacheProvider>();
     ```

4. In "Startup" class "Configure" method **initialize cache output**:

   ```csharp
   app.UseCacheOutput();
   ```
   
5. Decorate any controller method with cache output filters: 

```csharp
[CacheOutput(ClientTimeSpan = 0, ServerTimeSpan = 3600, MustRevalidate = true, ExcludeQueryStringFromCacheKey = false)]
```

6. Read https://github.com/filipw/Strathweb.CacheOutput for more details about common filter usage
