using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;
using Xunit;

namespace FlightTracker.Domain.Tests;

public class UserFlightTests
{
    [Fact]
    public void Defaults_AreSet()
    {
        var userFlight = new UserFlight();

        Assert.True(userFlight.DidFly);
        Assert.Equal(string.Empty, userFlight.SeatNumber);
        Assert.Null(userFlight.Notes);
        Assert.Equal(0, (int)userFlight.FlightClass);
    }
}
