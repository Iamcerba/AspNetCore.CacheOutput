using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.CacheOutput.Demo.InMemory.NET80.Controllers;

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

    [HttpGet(Name = "GetWeatherForecast")]
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
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)]
        })
        .ToArray();
    }
}
