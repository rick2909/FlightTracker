using System;
using System.Linq;
using System.Threading.Tasks;
using FlightTracker.Domain.Enums;
using FlightTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightTracker.Test.Infrastructure;

public class SeedDataTests
{
    private static FlightTrackerDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<FlightTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FlightTrackerDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_PopulatesCoreEntities()
    {
        using var context = CreateInMemoryContext();

        await SeedData.SeedAsync(context);

        Assert.NotEmpty(context.Airports);
        Assert.NotEmpty(context.Airlines);
        Assert.NotEmpty(context.Aircraft);
        Assert.NotEmpty(context.Flights);
        Assert.Contains(context.Flights, flight => flight.Status == FlightStatus.Cancelled);
    }
}
