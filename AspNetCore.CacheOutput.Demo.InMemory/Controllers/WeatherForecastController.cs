using System;
using System.Collections.Generic;
using System.Linq;
using AspNetCore.CacheOutput.Demo.InMemory.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AspNetCore.CacheOutput.Demo.InMemory.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [CacheOutput(
            AnonymousOnly = true,
            ClientTimeSpan = 0,
            ServerTimeSpan = 3600,
            MustRevalidate = true,
            ExcludeQueryStringFromCacheKey = false,
            CacheKeyGenerator = typeof(CustomCacheKeyGenerator)
        )]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = summaries[rng.Next(summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("error")]
        // Returns empty response on exception
        // Without cache returns: {"status":2,"good":false,"log":"abc"}
        [CacheOutput]
        public IEnumerable<WeatherForecast> GetError()
        {
            throw new BusinessException("abc");
        }
    }
}
