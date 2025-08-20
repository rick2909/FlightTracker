using FlightTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlightTracker.Infrastructure.Data;

/// <summary>
/// Design-time factory for creating DbContext instances during migrations.
/// </summary>
public class FlightTrackerDbContextFactory : IDesignTimeDbContextFactory<FlightTrackerDbContext>
{
    public FlightTrackerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FlightTrackerDbContext>();
        
    // Use SQLite for design-time migrations to align with development
    optionsBuilder.UseSqlite("Data Source=flighttracker.dev.db");

        return new FlightTrackerDbContext(optionsBuilder.Options);
    }
}
