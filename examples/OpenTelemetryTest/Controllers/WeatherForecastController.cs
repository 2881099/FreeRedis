using FreeRedis;
using Microsoft.AspNetCore.Mvc;

namespace OpenTelemetryTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IRedisClient _redisClient;
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IRedisClient redisClient)
        {
            _logger = logger;
            _redisClient = redisClient;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            var response = Enumerable.Range(1, 20).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray();

            await _redisClient.SetAsync("test01", response, 60 * 5);
            await _redisClient.GetAsync("test01");

            var cache = response.GroupBy(x => x.Summary).ToDictionary(g => g.Key!, g => g.ToList());
            await _redisClient.MSetAsync(cache);

            var cache1 = await _redisClient.GetAsync<List<WeatherForecast>>(Summaries[Random.Shared.Next(Summaries.Length)]);

            return response;
        }
    }
}
