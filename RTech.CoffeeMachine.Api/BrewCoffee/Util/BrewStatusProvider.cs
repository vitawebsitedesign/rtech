namespace RTech.CoffeeMachine.Api.BrewCoffee.Util;

public class BrewStatusProvider : IBrewStatusProvider
{
    private readonly IWeatherProxy _weatherProxy;

    public BrewStatusProvider(IWeatherProxy weatherProxy)
    {
        _weatherProxy = weatherProxy;
    }

    public async Task<BrewStatus> GetBrewStatus()
    {
        var temp = await _weatherProxy.TryGetTemperature();
        if (!temp.HasValue)
        {
            throw new InvalidOperationException("Temperature retrieval failed");
        }

        if (temp > 30)
        {
            return BrewStatus.ReadyIced;
        }
        return BrewStatus.ReadyHot;
    }
}
