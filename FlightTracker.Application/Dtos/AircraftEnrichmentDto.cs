namespace FlightTracker.Application.Dtos;

/// <summary>
/// DTO for aircraft data returned from external lookup services.
/// </summary>
public record AircraftEnrichmentDto(
    string? Registration,
    string? Type,
    string? IcaoType,
    string? Manufacturer,
    string? ModeS,
    string? RegisteredOwner,
    string? RegisteredOwnerCountryIso,
    string? RegisteredOwnerCountry);
