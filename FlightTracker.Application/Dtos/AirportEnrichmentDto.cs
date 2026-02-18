namespace FlightTracker.Application.Dtos;

/// <summary>
/// DTO for enriched airport data from AirportDB API.
/// </summary>
public sealed record AirportEnrichmentDto(
    string? IataCode,
    string? IcaoCode,
    string? Name,
    string? Municipality,
    string? CountryName,
    string? CountryIsoCode,
    double? Latitude,
    double? Longitude,
    int? ElevationFeet,
    string? AirportType
);
