using Microsoft.AspNetCore.Mvc;
using RTech.CoffeeMachine.Api.BrewCoffee.Filters;
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

    public CoffeeMachineController(
        ILogger<CoffeeMachineController> logger,
        IDateTimeProvider dateTimeProvider,
        IBrewStatusProvider brewStatusProvider
    )
    {
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _brewStatusProvider = brewStatusProvider;
    }

    [ServiceFilter(typeof(TeapotFilter))]
    [ServiceFilter(typeof(BrewStatusUnavailableFilter))]
    [HttpGet("brew-coffee")]
    public async Task<IActionResult> GetBrewStatus()
    {
        BrewStatus status;
        try
        {
            status = await _brewStatusProvider.GetBrewStatus();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to retrieve brew status");
            return StatusCode(StatusCodes.Status502BadGateway, null);
        }
        catch (Exception)
        {
            throw;
        }

        if (status == BrewStatus.ReadyHot)
        {
            var prepared = GetPreparedTimestamp(_dateTimeProvider.GetLocalNow());
            return Ok(new BrewStatusResponse("Your piping hot coffee is ready", prepared));
        }
        else if (status == BrewStatus.ReadyIced)
        {
            var prepared = GetPreparedTimestamp(_dateTimeProvider.GetLocalNow());
            return Ok(new BrewStatusResponse("Your refreshing iced coffee is ready", prepared));
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
