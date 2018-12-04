using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.CacheOutput.Demo.InMemory.Controllers
{
    public class ValuesController : Controller
    {
        // GET api/values
        [HttpGet("api/values")]
        [CacheOutput(
            ClientTimeSpan = 0,
            ServerTimeSpan = 3600,
            MustRevalidate = true,
            ExcludeQueryStringFromCacheKey = false,
            CacheKeyGenerator = typeof(CustomCacheKeyGenerator)
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
            ExcludeQueryStringFromCacheKey = false,
            CacheKeyGenerator = typeof(CustomCacheKeyGenerator)
        )]
        public string GetValue(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost("api/values")]
        [InvalidateCacheOutput(nameof(ValuesController.GetValue), typeof(ValuesController))]
        [InvalidateCacheOutput(nameof(ValuesController.GetValues), typeof(ValuesController))]
        public void CreateValue([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("api/values/{id}")]
        [InvalidateCacheOutput(nameof(ValuesController.GetValue), typeof(ValuesController))]
        [InvalidateCacheOutput(nameof(ValuesController.GetValues), typeof(ValuesController))]
        public void UpdateValue(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("api/values/{id}")]
        [InvalidateCacheOutput(nameof(ValuesController.GetValue), typeof(ValuesController))]
        [InvalidateCacheOutput(nameof(ValuesController.GetValues), typeof(ValuesController))]
        public void DeleteValue(int id)
        {
        }
    }
}
