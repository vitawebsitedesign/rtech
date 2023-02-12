namespace RTech.CoffeeMachine.Api.BrewCoffee.Util;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime GetLocalNow()
    {
        // Interviewer notes:
        // * Another option is using DateTimeOffset instead of DateTime.
        // * It's ambiguous if the new endpoint needs to be tied to a specific timezone (e.g.: tokyo +09:00), or if can be tied to the operating system timezone.
        // * After clarifying the requirement with the product owner/stakeholder: if this coffee machine endpoint needs to handle a specific timezone, then i'd use DateTimeOffset instead.
        return DateTime.Now;
    }
}
