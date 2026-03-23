namespace FlightTracker.Web.Configuration;

public sealed class FlightTrackerApiOptions
{
    public const string SectionName = "FlightTrackerApi";

    public string? BaseUrl { get; set; }

    public ApiSliceRolloutOptions Slices { get; set; } = new();
}

public sealed class ApiSliceRolloutOptions
{
    public bool Airports { get; set; }
}
