using FlightTracker.Domain.Enums;

namespace FlightTracker.Application.Dtos;

/// <summary>
/// DTO for creating new aircraft.
/// </summary>
public record CreateAircraftDto
{
    public string Registration { get; init; } = string.Empty;
    public AircraftManufacturer Manufacturer { get; init; }
    public string Model { get; init; } = string.Empty;
    public int? YearManufactured { get; init; }
    public int? PassengerCapacity { get; init; }
    public string? IcaoTypeCode { get; init; }
    public string? Notes { get; init; }
    public int? AirlineId { get; init; }
    public string? AirlineIcaoCode { get; init; }
    public string? AirlineIataCode { get; init; }
}
