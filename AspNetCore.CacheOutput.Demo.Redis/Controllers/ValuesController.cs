using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.CacheOutput.Demo.Redis.Controllers
{
    public class ValuesController : Controller
    {
        // GET api/values
        [HttpGet("api/values")]
        [CacheOutput(
            ClientTimeSpan = 0,
            ServerTimeSpan = 3600,
            MustRevalidate = true,
            ExcludeQueryStringFromCacheKey = false
        )]
        public IEnumerable<string> GetValues()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("api/values/{id}")]
        [CacheOutput(
            ClientTimeSpan = 0,
            ServerTimeSpan = 3600,
            MustRevalidate = true,
            ExcludeQueryStringFromCacheKey = false
        )]
        public string GetValue(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost("api/values")]
        [CacheOutput.Redis.InvalidateCacheOutput(nameof(GetValue))]
        [CacheOutput.Redis.InvalidateCacheOutput(nameof(GetValues))]
        public void CreateValue([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("api/values/{id}")]
        [CacheOutput.Redis.InvalidateCacheOutput(
            typeof(ValuesController),
            nameof(GetValue),
            null,
            "id"
        )] // Invalidating just cache related to this document
        [CacheOutput.Redis.InvalidateCacheOutput(nameof(GetValues))]
        public void UpdateValue(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("api/values/{id}")]
        [CacheOutput.Redis.InvalidateCacheOutput(
            typeof(ValuesController),
            nameof(GetValue),
            null,
            "id"
        )] // Invalidating just cache related to this document
        [CacheOutput.Redis.InvalidateCacheOutput(nameof(GetValues))]
        public void DeleteValue(int id)
        {
        }
    }
}
