using System;
using Shouldly;

public class CoffeeMachineControllerTests
{
    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    public void GetBrewStatus_WhenEveryFifthCall_ShouldReturn503(int callNum)
    {
        false.ShouldBeTrue();
    }

    [Fact]
    public void GetBrewStatus_WhenApril1st_ShouldReturn418()
    {
        false.ShouldBeTrue();
    }

    [Fact]
    public void GetBrewStatus_WhenCoffeeReady_ShouldReturn200()
    {
        false.ShouldBeTrue();
    }
}
