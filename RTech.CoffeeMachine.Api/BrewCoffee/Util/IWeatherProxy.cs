namespace RTech.CoffeeMachine.Api.BrewCoffee.Util;

public interface IWeatherProxy
{
    Task<float?> TryGetTemperature();
}
