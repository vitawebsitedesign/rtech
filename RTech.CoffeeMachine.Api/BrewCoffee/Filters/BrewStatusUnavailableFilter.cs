using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using RTech.CoffeeMachine.Api.BrewCoffee.Controllers;

namespace RTech.CoffeeMachine.Api.BrewCoffee.Filters;

public class BrewStatusUnavailableFilter : ActionFilterAttribute
{
    // Interviewer note: MemoryCache from Microsoft.Extensions.Caching.Abstractions is thread safe
    private readonly IMemoryCache _cache;
    public static readonly string CACHE_KEY_NUM_REQUESTS = $"{nameof(CoffeeMachineController.GetBrewStatus)}:numRequests";

    public BrewStatusUnavailableFilter(IMemoryCache cache)
    {
        _cache = cache;
        if (!_cache.TryGetValue<long>(CACHE_KEY_NUM_REQUESTS, out var _))
        {
            _cache.Set<long>(CACHE_KEY_NUM_REQUESTS, 0);
        }
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Interviewer note: I'm assuming that on april 1st, every 5th request still returns 418
        var numRequests = _cache.Get<long>(CACHE_KEY_NUM_REQUESTS);
        numRequests++;
        _cache.Set(CACHE_KEY_NUM_REQUESTS, numRequests);
        if (numRequests % 5 == 0)
        {
            context.Result = new ObjectResult(null)
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }

        base.OnActionExecuting(context);
    }
}

