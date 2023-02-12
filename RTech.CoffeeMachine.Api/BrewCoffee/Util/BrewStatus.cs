namespace RTech.CoffeeMachine.Api.BrewCoffee.Util;

// Interviewer notes:
// * 0 is just an unused enum default value (for good measure).
// * I'm using this enum (instead of a boolean) to improve code readability, but more importantly to improve code adaptability in anticipation of future code changes.
public enum BrewStatus
{
    None = 0,
    Ready = 1
}
