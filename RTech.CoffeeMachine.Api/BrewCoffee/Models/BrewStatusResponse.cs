namespace RTech.CoffeeMachine.Api.BrewCoffee.Models;

// Interviewer notes:
// * This record is a reference type (i.e.: wont cause memory issues when used as a parameter).
public record BrewStatusResponse(string Message, string Prepared);
