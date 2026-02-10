using System.Linq;
using System.Threading.Tasks;
using FlightTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightTracker.Infrastructure.Tests;

public class SeedDataTests
{
    private static FlightTrackerDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<FlightTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new FlightTrackerDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_ForCoreEntities()
    {
        using var ctx1 = CreateInMemoryContext("seed-idempotent");
        await SeedData.SeedAsync(ctx1);

        var airportsCount1 = ctx1.Airports.Count();
        var airlinesCount1 = ctx1.Airlines.Count();
        var aircraftCount1 = ctx1.Aircraft.Count();
        var flightsCount1 = ctx1.Flights.Count();

        await SeedData.SeedAsync(ctx1);

        Assert.Equal(airportsCount1, ctx1.Airports.Count());
        Assert.Equal(airlinesCount1, ctx1.Airlines.Count());
        Assert.Equal(aircraftCount1, ctx1.Aircraft.Count());
        Assert.Equal(flightsCount1, ctx1.Flights.Count());
    }
}
