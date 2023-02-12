using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using RTech.CoffeeMachine.Api.BrewCoffee.Controllers;
using RTech.CoffeeMachine.Api.BrewCoffee.Models;
using RTech.CoffeeMachine.Api.BrewCoffee.Util;
using Shouldly;

namespace RTech.CoffeeMachine.Api.Tests.BrewCoffee.Controllers;

public class CoffeeMachineControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CoffeeMachineControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetBrewStatus_WhenUnrecognizedBrewStatus_ShouldReturn500()
    {
        var brewStatusProvider = new Mock<IBrewStatusProvider>();
        brewStatusProvider.Setup(p => p.GetBrewStatus())
            .Returns(BrewStatus.None);

        var response = await GetEndpointResponse(services =>
        {
            services.AddSingleton<IBrewStatusProvider>(brewStatusProvider.Object);
        });

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task GetBrewStatus_WhenException_ShouldReturn500()
    {
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(p => p.GetLocalNow())
            .Throws<InvalidOperationException>();

        var response = await GetEndpointResponse(services =>
        {
            services.AddSingleton<IDateTimeProvider>(dateTimeProvider.Object);
        });

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task GetBrewStatus_WhenBrewStatusUnavailable_ShouldReturnEmpty503()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        long numRequestsSoFar = 4;
        cache.Set(CoffeeMachineController.CACHE_KEY_NUM_REQUESTS, numRequestsSoFar);
        var response = await GetEndpointResponse(services =>
        {
            services.AddSingleton<IMemoryCache>(cache);
        });

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData(0, StatusCodes.Status200OK)]
    [InlineData(1, StatusCodes.Status200OK)]
    [InlineData(2, StatusCodes.Status200OK)]
    [InlineData(3, StatusCodes.Status200OK)]
    [InlineData(4, StatusCodes.Status503ServiceUnavailable)]
    [InlineData(5, StatusCodes.Status200OK)]
    public async Task GetBrewStatus_WhenEndpointCalled_ShouldReturnCorrectStatusCode(long numRequestsSoFar, int expectedStatusCode)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set(CoffeeMachineController.CACHE_KEY_NUM_REQUESTS, numRequestsSoFar);
        var response = await GetEndpointResponse(services =>
        {
            services.AddSingleton<IMemoryCache>(cache);
        });

        response.ShouldNotBeNull();
        ((int)response.StatusCode).ShouldBe(expectedStatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task GetBrewStatus_WhenApril1st_ShouldReturnEmpty418(long numRequestsSoFar)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set(CoffeeMachineController.CACHE_KEY_NUM_REQUESTS, numRequestsSoFar);

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(p => p.GetLocalNow())
            .Returns(new DateTime(2023, 4, 1));

        var response = await GetEndpointResponse(services =>
        {
            services.AddSingleton<IMemoryCache>(cache);
            services.AddSingleton<IDateTimeProvider>(dateTimeProvider.Object);
        });

        ((int)response.StatusCode).ShouldBe(StatusCodes.Status418ImATeapot);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task GetBrewStatus_WhenCoffeeReady_ShouldReturn200()
    {
        var nowLocal = new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Local);
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(p => p.GetLocalNow())
            .Returns(nowLocal);

        var response = await GetEndpointResponse(services =>
        {
            services.AddSingleton<IDateTimeProvider>(dateTimeProvider.Object);
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<BrewStatusResponse>();
        content.ShouldNotBeNull();
        content.Message.ShouldBe("Your piping hot coffee is ready");

        var offsetStr = DateTime.Now.ToString("zzz").Replace(":", "");
        var expectedPrepared = $"2023-01-02T03:04:05{offsetStr}";
        content.Prepared.ShouldBe(expectedPrepared);
    }

    private Task<HttpResponseMessage> GetEndpointResponse(Action<IServiceCollection> servicesConfiguration)
    {
        var mockFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(servicesConfiguration);
        });

        var client = mockFactory.CreateClient();
        return client.GetAsync("brew-coffee");
    }
}
