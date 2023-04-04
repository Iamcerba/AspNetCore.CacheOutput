# AspNetCore.CacheOutput.InMemory

In-memory cache provider implementation for [AspNetCore.CacheOutput](https://github.com/Iamcerba/AspNetCore.CacheOutput)

### Installation

1. Install core package: **Install-Package AspNetCore.CacheOutput**

2. Install package with in-memory cache provider implementation: **Install-Package AspNetCore.CacheOutput.InMemory**

3. In "Startup" class "ConfigureServices" method register additional services:

   ```csharp
   services.AddInMemoryCacheOutput();
   ```

### Usage

See https://github.com/Iamcerba/AspNetCore.CacheOutput for more details.
