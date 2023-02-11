using System;
using Microsoft.AspNetCore.Mvc;

namespace RTech.CoffeeMachine.Api.Controllers;

[ApiController]
[Route("")]
public class CoffeeMachineController : ControllerBase
{
	public CoffeeMachineController()
	{
	}

	[HttpGet("brew-coffee")]
	public IActionResult GetBrewStatus()
	{
		throw new NotImplementedException();
	}
}
