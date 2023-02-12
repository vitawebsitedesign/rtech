using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RTech.CoffeeMachine.Api.BrewCoffee.Util;

namespace RTech.CoffeeMachine.Api.BrewCoffee.Filters;

public class TeapotFilter : ActionFilterAttribute
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public TeapotFilter(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Interviewer note: I'm assuming that the april fools check uses local time (operating system time)
        var localNow = _dateTimeProvider.GetLocalNow();
        var isApril1st = localNow.Month == 4 && localNow.Day == 1;
        if (isApril1st)
        {
            context.Result = new ObjectResult(null)
            {
                StatusCode = StatusCodes.Status418ImATeapot
            };
        }

        base.OnActionExecuting(context);
    }
}

