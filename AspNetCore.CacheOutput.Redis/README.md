# AspNetCore.CacheOutput.Redis

StackExchange Redis cache provider implementation for [AspNetCore.CacheOutput](https://github.com/Iamcerba/AspNetCore.CacheOutput)

### Installation

1. Install core package: **Install-Package AspNetCore.CacheOutput**

2. Install package with Redis cache provider implementation: **Install-Package AspNetCore.CacheOutput.Redis**

3. In "Startup" class "ConfigureServices" method register additional services:

   ```csharp
   services.AddRedisCacheOutput(Configuration.GetConnectionString("<redis connection string name>"));
   ```

### Usage

See https://github.com/Iamcerba/AspNetCore.CacheOutput for more details.
