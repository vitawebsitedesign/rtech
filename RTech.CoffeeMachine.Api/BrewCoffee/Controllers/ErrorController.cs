using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RTech.CoffeeMachine.Api.BrewCoffee.Controllers;

// Interviewer notes:
// * Whilst error-handling is not mentioned in the spec, i'm assuming that we should always handle unexpected errors anyways.
// * Depending on the company/team, they may already have a preferred error-handling convention, whether it be middleware or Hellang.
// * For now, I've just done this simple error handler for the time being.
public class ErrorController : ControllerBase
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [Route("/error")]
    public IActionResult HandleError()
    {
        var ex = HttpContext.Features.Get<IExceptionHandlerFeature>()!.Error;
        _logger.LogError(ex, ex.Message);
        return StatusCode(StatusCodes.Status500InternalServerError, null);
    }
}
