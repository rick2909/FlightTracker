namespace FlightTracker.Web.Configuration;

public sealed class FlightTrackerApiOptions
{
    public const string SectionName = "FlightTrackerApi";

    public string? BaseUrl { get; set; }

    public ApiSliceRolloutOptions Slices { get; set; } = new();

    public FirstPartyApiAuthOptions FirstPartyAuth { get; set; } = new();
}

public sealed class ApiSliceRolloutOptions
{
    public bool Airports { get; set; }
    public bool Flights { get; set; }
    public bool Passport { get; set; }
    public bool Settings { get; set; }
}

public sealed class FirstPartyApiAuthOptions
{
    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int TokenLifetimeMinutes { get; set; } = 15;
}
