using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.CacheOutput.Demo.Redis.NET90.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [HttpGet(Name = "GetWeatherForecast")]
        [CacheOutput(
            ClientTimeSpan = 0,
            ServerTimeSpan = 3600,
            MustRevalidate = true,
            ExcludeQueryStringFromCacheKey = false
        )]
        public IEnumerable<WeatherForecast> GetValues()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

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

        [HttpPost("api/values")]
        [CacheOutput.Redis.InvalidateCacheOutput(nameof(GetValue))]
        [CacheOutput.Redis.InvalidateCacheOutput(nameof(GetValues))]
        public void CreateValue([FromBody] string value)
        {
        }

        [HttpPut("api/values/{id}")]
        [CacheOutput.Redis.InvalidateCacheOutput(
            typeof(WeatherForecastController),
            nameof(GetValue),
            null,
            "id"
        )] // Invalidating just cache related to this document
        [CacheOutput.Redis.InvalidateCacheOutput(nameof(GetValues))]
        public void UpdateValue(int id, [FromBody] string value)
        {
        }
    }
}
