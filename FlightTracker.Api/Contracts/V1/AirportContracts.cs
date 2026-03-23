namespace FlightTracker.Api.Contracts.V1;

public sealed record AirportResponse(
    int Id,
    string Name,
    string City,
    string Country,
    string? IataCode,
    string? IcaoCode,
    double? Latitude,
    double? Longitude,
    string? TimeZoneId);

public sealed record AirportFlightListItemResponse(
    int? Id,
    string FlightNumber,
    string? Airline,
    string? Aircraft,
    string? DepartureTimeUtc,
    string? ArrivalTimeUtc,
    string? DepartureCode,
    string? ArrivalCode);

public sealed record AirportFlightsResponse(
    IEnumerable<AirportFlightListItemResponse> Departing,
    IEnumerable<AirportFlightListItemResponse> Arriving);
