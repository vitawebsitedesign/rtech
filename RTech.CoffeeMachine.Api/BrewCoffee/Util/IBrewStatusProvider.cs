namespace RTech.CoffeeMachine.Api.BrewCoffee.Util;

/// <summary>
/// Represents an abstraction that can provide a brew status
/// </summary>
public interface IBrewStatusProvider
{
    BrewStatus GetBrewStatus();
}
