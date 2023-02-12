namespace RTech.CoffeeMachine.Api.BrewCoffee.Util;

public class BrewStatusProvider : IBrewStatusProvider
{
    public BrewStatus GetBrewStatus() => BrewStatus.Ready;
}
