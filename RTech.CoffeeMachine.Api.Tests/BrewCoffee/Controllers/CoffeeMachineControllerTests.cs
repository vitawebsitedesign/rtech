using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using RichardSzalay.MockHttp;
using RTech.CoffeeMachine.Api.BrewCoffee.Filters;
using RTech.CoffeeMachine.Api.BrewCoffee.Models;
using RTech.CoffeeMachine.Api.BrewCoffee.Util;
using Shouldly;
using static RTech.CoffeeMachine.Api.BrewCoffee.Models.TemperatureResponse;

namespace RTech.CoffeeMachine.Api.Tests.BrewCoffee.Controllers;

public class CoffeeMachineControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string MOCKED_HTTPCLIENT_BASE_URL = "https://a.com";
    private const string BREW_READY_HOT_MESSAGE = "Your piping hot coffee is ready";
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
            .ReturnsAsync(BrewStatus.None);

        var response = await GetEndpointResponse(brewStatusProvider: brewStatusProvider.Object);

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

        var response = await GetEndpointResponse(dateTimeProvider: dateTimeProvider.Object);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task GetBrewStatus_WhenBrewStatusUnavailable_ShouldReturnEmpty503()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        long numRequestsSoFar = 4;
        cache.Set(BrewStatusUnavailableFilter.CACHE_KEY_NUM_REQUESTS, numRequestsSoFar);
        var response = await GetEndpointResponse(cache: cache);

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
        cache.Set(BrewStatusUnavailableFilter.CACHE_KEY_NUM_REQUESTS, numRequestsSoFar);
        var response = await GetEndpointResponse(cache: cache);

        response.ShouldNotBeNull();
        ((int)response.StatusCode).ShouldBe(expectedStatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task GetBrewStatus_WhenApril1st_ShouldReturnEmpty418(long numRequestsSoFar)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set(BrewStatusUnavailableFilter.CACHE_KEY_NUM_REQUESTS, numRequestsSoFar);

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(p => p.GetLocalNow())
            .Returns(new DateTime(2023, 4, 1));

        var response = await GetEndpointResponse(
            cache: cache,
            dateTimeProvider: dateTimeProvider.Object
        );

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

        var response = await GetEndpointResponse(dateTimeProvider: dateTimeProvider.Object);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<BrewStatusResponse>();
        content.ShouldNotBeNull();
        content.Message.ShouldBe(BREW_READY_HOT_MESSAGE);

        var offsetStr = DateTime.Now.ToString("zzz").Replace(":", "");
        var expectedPrepared = $"2023-01-02T03:04:05{offsetStr}";
        content.Prepared.ShouldBe(expectedPrepared);
    }

    [Fact]
    public async Task GetBrewStatus_WhenTemperatureRequestThrowsException_ShouldReturn502()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*")
            .Throw(new HttpRequestException());

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(MOCKED_HTTPCLIENT_BASE_URL);
        var response = await GetEndpointResponse(httpClient: httpClient);

        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task GetBrewStatus_WhenTemperatureRequestReturns500_ShouldReturn502()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(MOCKED_HTTPCLIENT_BASE_URL);
        var response = await GetEndpointResponse(httpClient: httpClient);

        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData(30, BREW_READY_HOT_MESSAGE)]
    [InlineData(30.01, "Your refreshing iced coffee is ready")]
    public async Task GetBrewStatus_WhenTemperatureRetrieved_ShouldReturnCorrectMessage(float temperature, string expectedMessage)
    {
        var httpClient = CreateMockedHttpClient(temperature);
        var response = await GetEndpointResponse(httpClient: httpClient);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<BrewStatusResponse>();
        content.ShouldNotBeNull();
        content.Message.ShouldBe(expectedMessage);
    }

    private Task<HttpResponseMessage> GetEndpointResponse(
        IDateTimeProvider? dateTimeProvider = null,
        IBrewStatusProvider? brewStatusProvider = null,
        IMemoryCache? cache = null,
        HttpClient? httpClient = null
    )
    {
        var mockFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                if (dateTimeProvider != null)
                    services.AddSingleton<IDateTimeProvider>(dateTimeProvider);
                if (brewStatusProvider != null)
                    services.AddSingleton<IBrewStatusProvider>(brewStatusProvider);
                if (cache != null)
                    services.AddSingleton<IMemoryCache>(cache);

                httpClient ??= CreateMockedHttpClient(30);
                services.AddHttpClient<IWeatherProxy, WeatherProxy>(client => new WeatherProxy(
                    Mock.Of<ILogger<WeatherProxy>>(),
                    httpClient
                ));
            });
        });

        var client = mockFactory.CreateClient();
        return client.GetAsync("brew-coffee");
    }

    private static HttpClient CreateMockedHttpClient(float temperature)
    {
        var responseJson = JsonSerializer.Serialize(new TemperatureResponse
        {
            Weather = new CurrentWeather
            {
                Temperature = temperature
            }
        });

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*")
            .Respond(MediaTypeNames.Application.Json, responseJson);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(MOCKED_HTTPCLIENT_BASE_URL);  // URL not actually called, just needs a base address
        return httpClient;
    }
}
