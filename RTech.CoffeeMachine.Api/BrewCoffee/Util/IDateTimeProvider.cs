namespace RTech.CoffeeMachine.Api.BrewCoffee.Util;

/// <summary>
/// Represents an abstraction that can provide date+time.
/// </summary>
/// <remarks>
/// Enhances code testability (interface can be mocked).
/// </remarks>
public interface IDateTimeProvider
{
    DateTime GetLocalNow();
}
