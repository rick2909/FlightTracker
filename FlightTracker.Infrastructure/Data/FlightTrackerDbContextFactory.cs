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
        
        // Use a default connection string for migrations
        // In a real application, this would come from configuration
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=FlightTrackerDb;Trusted_Connection=true;MultipleActiveResultSets=true");

        return new FlightTrackerDbContext(optionsBuilder.Options);
    }
}
