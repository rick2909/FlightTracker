namespace FlightTracker.Web.Api.Contracts;

public sealed record AirportApiResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string? IataCode { get; init; }
    public string? IcaoCode { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? TimeZoneId { get; init; }
}

public sealed record AirportFlightListItemApiResponse
{
    public int? Id { get; init; }
    public string FlightNumber { get; init; } = string.Empty;
    public string? Airline { get; init; }
    public string? Aircraft { get; init; }
    public string? DepartureTimeUtc { get; init; }
    public string? ArrivalTimeUtc { get; init; }
    public string? DepartureCode { get; init; }
    public string? ArrivalCode { get; init; }
}

public sealed record AirportFlightsApiResponse
{
    public List<AirportFlightListItemApiResponse> Departing { get; init; } = new();
    public List<AirportFlightListItemApiResponse> Arriving { get; init; } = new();
}
