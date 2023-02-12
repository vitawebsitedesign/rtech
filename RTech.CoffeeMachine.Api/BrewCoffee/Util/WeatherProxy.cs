using RTech.CoffeeMachine.Api.BrewCoffee.Models;

namespace RTech.CoffeeMachine.Api.BrewCoffee.Util;

public class WeatherProxy : IWeatherProxy
{
    private readonly ILogger<WeatherProxy> _logger;
    private readonly HttpClient _httpClient;
    // Interviewer notes:
    // * I'm choosing to use a const here, since theres nothing to change at run-time.
    // * If the url had dynamic variables & had to change at run-time, i'd use UriBuilder or an equivalent to construct the url.
    public const string WEATHER_URL = "/v1/forecast?latitude=-37.81&longitude=144.96&hourly=temperature_2m&current_weather=true";

    public WeatherProxy(ILogger<WeatherProxy> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <returns>
    /// Temperature in metric, null if failed to retrieve data
    /// </returns>
    public async Task<float?> TryGetTemperature()
    {
        _logger.LogInformation("Retrieving temperature..");
        try
        {
            // Interviewer notes:
            // * i'm assuming hard-coded latitude longitude is permitted (i.e.: not accessing gps or server location geolocation at run-time)
            // * this api uses the open-source https://open-meteo.com/
            using (var response = await _httpClient.GetAsync(WEATHER_URL))
            {
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<TemperatureResponse>();
                var temp = result?.Weather?.Temperature;
                if (!temp.HasValue)
                {
                    throw new InvalidOperationException("Weather provider data source did not return a temperature");
                }

                _logger.LogInformation($"Retrieved, temp={temp}");
                return temp;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve temperature");
            return null;
        }
    }
}
