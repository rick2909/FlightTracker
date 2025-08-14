namespace FlightTracker.Application.Dtos;

/// <summary>
/// DTO for user flight statistics.
/// </summary>
public record UserFlightStatsDto
{
    public int UserId { get; init; }
    public int TotalFlights { get; init; }
    public int EconomyFlights { get; init; }
    public int PremiumEconomyFlights { get; init; }
    public int BusinessFlights { get; init; }
    public int FirstClassFlights { get; init; }
    public int UniqueAirports { get; init; }
    public int UniqueCountries { get; init; }
    public int TotalTravelTimeInMinutes { get; init; }
    public IEnumerable<TravelTimeDto> TravelTimes { get; init; } = Enumerable.Empty<TravelTimeDto>();
}
