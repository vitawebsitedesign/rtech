using System.Text.Json.Serialization;

namespace RTech.CoffeeMachine.Api.BrewCoffee.Models;

public class TemperatureResponse
{
    [JsonPropertyName("current_weather")]
    public CurrentWeather? Weather { get; set; }

    public class CurrentWeather
    {
        public float Temperature { get; set; }
    }
}


