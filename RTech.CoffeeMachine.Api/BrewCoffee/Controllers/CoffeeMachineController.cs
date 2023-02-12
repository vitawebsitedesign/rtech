using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using RTech.CoffeeMachine.Api.BrewCoffee.Models;
using RTech.CoffeeMachine.Api.BrewCoffee.Util;

namespace RTech.CoffeeMachine.Api.BrewCoffee.Controllers;

[ApiController]
[Route("")]
public class CoffeeMachineController : ControllerBase
{
    private readonly ILogger<CoffeeMachineController> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBrewStatusProvider _brewStatusProvider;
    // Interviewer note: MemoryCache from Microsoft.Extensions.Caching.Abstractions is thread safe
    private readonly IMemoryCache _cache;
    public static readonly string CACHE_KEY_NUM_REQUESTS = $"{nameof(CoffeeMachineController)}:numRequests";

    public CoffeeMachineController(
        ILogger<CoffeeMachineController> logger,
        IDateTimeProvider dateTimeProvider,
        IBrewStatusProvider brewStatusProvider,
        IMemoryCache cache
    )
    {
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _brewStatusProvider = brewStatusProvider;
        _cache = cache;

        if (!_cache.TryGetValue<long>(CACHE_KEY_NUM_REQUESTS, out var _))
        {
            _cache.Set<long>(CACHE_KEY_NUM_REQUESTS, 0);
        }
    }

    [HttpGet("brew-coffee")]
    public IActionResult GetBrewStatus()
    {
        // Interviewer note: I'm assuming that the april fools check uses local time (operating system time)
        var localNow = _dateTimeProvider.GetLocalNow();
        var isApril1st = localNow.Month == 4 && localNow.Day == 1;
        if (isApril1st)
        {
            return StatusCode(StatusCodes.Status418ImATeapot, null);
        }

        // Interviewer note: I'm assuming that on april 1st, every 5th request still returns 418
        var numRequests = _cache.Get<long>(CACHE_KEY_NUM_REQUESTS);
        numRequests++;
        _cache.Set(CACHE_KEY_NUM_REQUESTS, numRequests);
        if (numRequests % 5 == 0)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, null);
        }

        var status = _brewStatusProvider.GetBrewStatus();
        if (status == BrewStatus.Ready)
        {
            var prepared = GetPreparedTimestamp(localNow);
            return Ok(new BrewStatusResponse("Your piping hot coffee is ready", prepared));
        }
        else
        {
            // Interviewer note: The below exception subsequently gets handled in ErrorController.cs
            throw new InvalidOperationException($"Unrecognized brew status: {status}");
        }
    }

    private string GetPreparedTimestamp(DateTime localNow)
    {
        // Interviewer notes:
        // * The example timestamp in the spec shows a (valid) variant of iso 8601, where the offset does not include a separator.
        //   * Hence the built-in .NET "K" & "o"/"O" format strings cannot be used.
        //   * Additionally, whilst the sample given can be parsed by System.DateTime, it can NOT be deserialized by .NET's built-in HttpContent.ReadFromJsonAsync.
        // * Given the above, I would first clarify with the product owner/stakeholder on whether the provided ISO 8601 variant is correct.
        //   * If correct, I would implement middleware (i.e.: inherit from JsonConverter) to support offset formats without colons.
        //   * If not correct, i would simply use the built-in .net "K", "o" or "O" format strings.
        // * All things considered, I am assuming that the iso 8601 sample provided in the spec is correct & must be supported.
        var preparedTime = localNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
        var preparedOffset = localNow.ToString("zzz").Replace(":", "");
        return $"{preparedTime}{preparedOffset}";
    }
}
