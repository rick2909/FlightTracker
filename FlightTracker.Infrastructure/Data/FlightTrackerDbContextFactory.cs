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

        // Resolve db path to solution root regardless of current working directory.
        var currentDirectory = Directory.GetCurrentDirectory();
        var folderName = Path.GetFileName(currentDirectory);
        var solutionRoot = folderName.Equals("FlightTracker.Api", StringComparison.OrdinalIgnoreCase)
            || folderName.Equals("FlightTracker.Web", StringComparison.OrdinalIgnoreCase)
            || folderName.Equals("FlightTracker.Infrastructure", StringComparison.OrdinalIgnoreCase)
                ? Directory.GetParent(currentDirectory)?.FullName ?? currentDirectory
                : currentDirectory;
        var dbPath = Path.Combine(solutionRoot, "flighttracker.dev.db");

        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new FlightTrackerDbContext(optionsBuilder.Options);
    }
}
