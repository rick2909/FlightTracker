using FlightTracker.Domain.Enums;

namespace FlightTracker.Application.Dtos;

/// <summary>
/// DTO for aircraft data transfer.
/// </summary>
public record AircraftDto
{
    public int Id { get; init; }
    public string Registration { get; init; } = string.Empty;
    public AircraftManufacturer Manufacturer { get; init; }
    public string Model { get; init; } = string.Empty;
    public int? YearManufactured { get; init; }
    public int? PassengerCapacity { get; init; }
    public string? IcaoTypeCode { get; init; }
    public string? ModeS { get; init; }
    public string? Notes { get; init; }
    public int? AirlineId { get; init; }
    public string? AirlineIcaoCode { get; init; }
    public string? AirlineIataCode { get; init; }
    public string? AirlineName { get; init; }
}
