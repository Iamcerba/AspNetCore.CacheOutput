# AspNetCore.CacheOutput

Strathweb.CacheOutput library (https://github.com/filipw/Strathweb.CacheOutput) redone to work with ASP.NET Core

**CacheOutput** will take care of server side caching and set the appropriate client side (response) headers for you.

You can specify the following properties:
 - *ClientTimeSpan* (corresponds to CacheControl MaxAge HTTP header)
 - *MustRevalidate* (corresponds to MustRevalidate HTTP header - indicates whether the origin server requires revalidation of 
a cache entry on any subsequent use when the cache entry becomes stale)
 - *ExcludeQueryStringFromCacheKey* (do not vary cache by querystring values)
 - *ServerTimeSpan* (time how long the response should be cached on the server side)
 - *AnonymousOnly* (cache enabled only for requests when Thread.CurrentPrincipal is not set)
 
Additionally, the library is setting ETags for you, and keeping them unchanged for the duration of the caching period.
Caching by default can only be applied to GET actions.

### Installation

You can build from the source here, or you can install the Nuget version:

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

### Usage

```csharp
// Cache for 100 seconds on the server, inform the client that response is valid for 100 seconds
[CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
public IEnumerable<string> Get()
{
    return new string[] { "value1", "value2" };
}

// Cache for 100 seconds on the server, inform the client that response is valid for 100 seconds. Cache for anonymous users only.
[CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100, AnonymousOnly = true)]
public IEnumerable<string> Get()
{
    return new string[] { "value1", "value2" };
}

// Inform the client that response is valid for 50 seconds. Force client to revalidate.
[CacheOutput(ClientTimeSpan = 50, MustRevalidate = true)]
public string Get(int id)
{
    return "value";
}

// Cache for 50 seconds on the server. Ignore querystring parameters when serving cached content.
[CacheOutput(ServerTimeSpan = 50, ExcludeQueryStringFromCacheKey = true)]
public string Get(int id)
{
    return "value";
}
```

### Caching convention

In order to determine the expected content type of the response, **CacheOutput** will run Web APIs internal *content negotiation process*, based on the incoming request & the return type of the action on which caching is applied. 

Each individual content type response is cached separately (so out of the box, you can expect the action to be cached as JSON and XML, if you introduce more formatters, those will be cached as well).

**Important**: We use *action name* as part of the key. Therefore it is *necessary* that action names are unique inside the controller - that's the only way we can provide consistency. 

So you either should use unique method names inside a single controller, or (if you really want to keep them the same names when overloading) you need to use *ActionName* attribute to provide uniqeness for caching. Example:

```csharp
[CacheOutput(ClientTimeSpan = 50, ServerTimeSpan = 50)]
public IEnumerable<Team> Get()
{
    return Teams;
}

[ActionName("GetById")]
[CacheOutput(ClientTimeSpan = 50, ServerTimeSpan = 50)]
public IEnumerable<Team> Get(int id)
{
    return Teams;
}
```

If you want to bypass the content negotiation process, you can do so by using the `MediaType` property:

```csharp
[CacheOutput(ClientTimeSpan = 50, ServerTimeSpan = 50, MediaType = "image/jpeg")]
public HttpResponseMessage Get(int id)
{
    var response = new HttpResponseMessage(HttpStatusCode.OK);
    response.Content = GetImage(id); // e.g. StreamContent, ByteArrayContent,...
    response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
    
    return response;
}
```

This will always return a response with `image/jpeg` as value for the `Content-Type` header.

### Ignoring caching

You can set up caching globally (add global caching filter) or on controller level (decorate the controller with the caching attribute). This means that caching settings will cascade down to all the actions in your entire application (in the first case) or in the controller (in the second case).

You can still instruct a specific action to opt out from caching by using `[IgnoreCacheOutput]` attribute.

```csharp
[CacheOutput(ClientTimeSpan = 50, ServerTimeSpan = 50)]
public class IgnoreController : Controller
{
    [HttpGet("cached")]
    public string GetCached()
    {
        return DateTime.Now.ToString();
    }

    [IgnoreCacheOutput]
    [HttpGet("uncached")]
    public string GetUnCached()
    {
        return DateTime.Now.ToString();
    }
}
```

### Server-side caching

By default you can use AspNetCore.CacheOutput.InMemory cache provider to cache on the server side. However, you are free to swap this with anything else
(static Dictionary, Memcached, Redis, whatever..) as long as you implement the following *IApiCacheOutput* interface (part of the distributed assembly).

```csharp
public interface IApiCacheOutput
{
    Task RemoveStartsWithAsync(string key);
    Task<T> GetAsync<T>(string key) where T : class;
    Task RemoveAsync(string key);
    Task<bool> ContainsAsync(string key);
    Task AddAsync(string key, object value, DateTimeOffset expiration, string dependsOnKey = null);
}
```

Suppose you have a custom implementation:

```csharp
public class MyCache : IApiCacheOutput
{
    // omitted for brevity
}
```

You can register your implementation in "Startup" class "ConfigureServices" method:

```csharp
services.AddSingleton<IApiCacheOutput, MyCache>();
```

### Cache invalidation

Invalidation on action level - done through attributes. For example:

```csharp
public class TeamsController : Controller
{
    [CacheOutput(ClientTimeSpan = 50, ServerTimeSpan = 50)]
    public IEnumerable<Team> Get()
    {
        // return something
    }

    [CacheOutput(ClientTimeSpan = 50, ServerTimeSpan = 50)]
    public IEnumerable<Player> GetTeamPlayers(int id)
    {
        // return something
    }

    [InvalidateCacheOutput(nameof(Get))]
    public void Post(Team value)
    {
        // this invalidates Get action cache
    }
}
```

Obviously, multiple attributes are supported. You can also invalidate methods from separate controller:

```csharp
[InvalidateCacheOutput(typeof(OtherController), nameof(Get))] // this will invalidate Get in a different controller
[InvalidateCacheOutput(nameof(Get))] // this will invalidate Get in this controller
public void Post(Team value)
{
    // do stuff
}
```

If an error occurs that would prevent the resource from getting updated, return a non-Success status code to stop the invalidation:

```csharp
[InvalidateCacheOutput(nameof(Get)] // this will invalidate Get upon a successful request
public void Put(Team value)
{
    try{
      // do stuff that causes an error
    }
    catch (...specific error)
    {
       Response.StatusCode = (int)HttpStatusCode.BadRequest;  // Prevents clearing the cache for the controller(s)
    }
}
```
This works with `ActionResult`s as well:
```csharp
[InvalidateCacheOutput(nameof(Get)] // this will invalidate Get upon a successful request
public ActionResult Put(Team value)
{
    try{
      // do stuff that causes an error
    }
    catch (...specific error)
    {
       return BadRequest();  // Prevents clearing the cache for the controller(s)
    }
    return NoContent();
}
```
Please note that the `Forbiden()` `ActionResult` does not have a StatusCode causing it to fall back to the Response.StatusCode usage. Since `ActionResult` values aren't
merged into the `Response` until much later, the `Response.StatusCode` will be defaulted to 200 and cause the cache to be invalidated. Setting the `Response.StatusCode` 
to 403 will cause it to behave as expected.

### Customizing the cache keys

You can provide your own cache key generator. To do this, you need to implement the `ICacheKeyGenerator` interface. The default implementation should suffice in most situations.

When implementing, it is easiest to inherit your custom generator from the `DefaultCacheKeyGenerator` class.

To set your custom implementation as the default register your implementation in "Startup" class "ConfigureServices" method:

```csharp
services.AddSingleton<ICacheKeyGenerator, MyCustomCacheKeyGenerator>();
```

You can set a specific cache key generator for an action, using the `CacheKeyGenerator` property:

```csharp
[CacheOutput(CacheKeyGenerator=typeof(SuperNiceCacheKeyGenerator))]
```

**Important**: register it with your DI *as itself*:

```csharp
services.AddSingleton<SuperNiceCacheKeyGenerator, SuperNiceCacheKeyGenerator>();
```

Finding a matching cache key generator is done in this order:

1. Checks if CacheKeyGenerator property set in current action CacheOutputAttribute.
2. Default globally registered CacheKeyGenerator.
3. `DefaultCacheKeyGenerator`


### JSONP

We automatically exclude *callback* parameter from cache key to allow for smooth JSONP support. 

So:

    /api/something?abc=1&callback=jQuery1213

is cached as:

    /api/something?abc=1

Position of the *callback* parameter does not matter.

### Etags

For client side caching, in addition to *MaxAge*, we will issue Etags. You can use the Etag value to make a request with *If-None-Match* header. If the resource is still valid, server will then response with a 304 status code.

For example:

    GET /api/myresource
    Accept: application/json

    Status Code: 200
    Cache-Control: max-age=100
    Content-Length: 24
    Content-Type: application/json; charset=utf-8
    Date: Fri, 25 Jan 2013 03:37:11 GMT
    ETag: "5c479911-97b9-4b78-ae3e-d09db420d5ba"
    Server: Microsoft-HTTPAPI/2.0

On the next request:

    GET /api/myresource
    Accept: application/json
    If-None-Match: "5c479911-97b9-4b78-ae3e-d09db420d5ba"
    
    Status Code: 304
    Cache-Control: max-age=100
    Content-Length: 0
    Date: Fri, 25 Jan 2013 03:37:13 GMT
    Server: Microsoft-HTTPAPI/2.0

### License

Licensed under MIT. License included.
