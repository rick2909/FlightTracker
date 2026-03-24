namespace FlightTracker.Api.Contracts.V1;

/// <summary>Represents a single airport in an API response.</summary>
/// <param name="Id">Internal airport identifier.</param>
/// <param name="Name">Full name of the airport.</param>
/// <param name="City">City the airport serves.</param>
/// <param name="Country">Country the airport is located in.</param>
/// <param name="IataCode">Three-letter IATA code, when known.</param>
/// <param name="IcaoCode">Four-letter ICAO code, when known.</param>
/// <param name="Latitude">WGS-84 latitude, when available.</param>
/// <param name="Longitude">WGS-84 longitude, when available.</param>
/// <param name="TimeZoneId">IANA time-zone identifier, when available.</param>
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

/// <summary>A single flight listed under an airport's departure or arrival schedule.</summary>
/// <param name="Id">Internal flight identifier, when available.</param>
/// <param name="FlightNumber">IATA or ICAO flight number.</param>
/// <param name="Airline">Operating airline name, when known.</param>
/// <param name="Aircraft">Aircraft type or registration, when known.</param>
/// <param name="DepartureTimeUtc">Scheduled departure time in UTC (ISO-8601 string).</param>
/// <param name="ArrivalTimeUtc">Scheduled arrival time in UTC (ISO-8601 string).</param>
/// <param name="DepartureCode">Departure airport IATA/ICAO code.</param>
/// <param name="ArrivalCode">Arrival airport IATA/ICAO code.</param>
public sealed record AirportFlightListItemResponse(
    int? Id,
    string FlightNumber,
    string? Airline,
    string? Aircraft,
    string? DepartureTimeUtc,
    string? ArrivalTimeUtc,
    string? DepartureCode,
    string? ArrivalCode);

/// <summary>Departure and arrival flight lists for a given airport.</summary>
/// <param name="Departing">Flights departing from this airport.</param>
/// <param name="Arriving">Flights arriving at this airport.</param>
public sealed record AirportFlightsResponse(
    IEnumerable<AirportFlightListItemResponse> Departing,
    IEnumerable<AirportFlightListItemResponse> Arriving);
