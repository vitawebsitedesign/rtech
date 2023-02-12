using RTech.CoffeeMachine.Api.BrewCoffee.Filters;
using RTech.CoffeeMachine.Api.BrewCoffee.Util;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
services.AddMemoryCache();
services.AddSingleton<IBrewStatusProvider, BrewStatusProvider>();
services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
services.AddSingleton<TeapotFilter>();
services.AddSingleton<BrewStatusUnavailableFilter>();

services.AddHttpClient<IWeatherProxy, WeatherProxy>(client =>
{
    var weatherProviderBaseAddress = builder.Configuration.GetValue<string>("WeatherProviderBaseAddress");
    if (string.IsNullOrWhiteSpace(weatherProviderBaseAddress))
    {
        throw new InvalidOperationException("Missing config for WeatherProviderBaseAddress");
    }

    client.BaseAddress = new Uri(weatherProviderBaseAddress);
}).SetHandlerLifetime(Timeout.InfiniteTimeSpan);    // Weather API does not require auth (e.g.: no oauth token expiry)

services.AddControllers();

var app = builder.Build();
app.UseHttpsRedirection();
// Interviewer note: please see ErrorController.cs
app.UseExceptionHandler("/error");
// Interviewer note: i'm assuming the endpoint doesn't need auth (e.g.: jwt+oauth)
app.UseAuthorization();
app.MapControllers();

app.Run();

// Interviewer note: this allows integration tests to bootstrap off of Program.cs (DI setup etc)
public partial class Program { }
