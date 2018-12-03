# AspNetCore.CacheOutput

ASP.NET Core port of Strathweb.CacheOutput library (https://github.com/filipw/Strathweb.CacheOutput)

### Initial configuration:

1. Install core package: **Install-Package AspNetCore.CacheOutput**

2. Depending on which cache provider you decided to use install any of the following packages:

   * **Install-Package AspNetCore.CacheOutput.InMemory**

   * **Install-Package AspNetCore.CacheOutput.Redis**

3. In "Startup" class "ConfigureServices" method depending on previosly installed cache provider register additional services:

   * For AspNetCore.CacheOutput.InMemory cache provider:
   
     ```csharp
     services.AddInMemoryCacheOutput();
     ```
   
   * For AspNetCore.CacheOutput.Redis cache provider:
   
     ```csharp
     services.AddRedisCacheOutput(Configuration.GetConnectionString("<redis connection string name>"));
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
