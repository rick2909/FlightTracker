using FlightTracker.Application.Results;
using FlightTracker.Application.Dtos;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Web.Services.Interfaces;

public interface IAirportsSliceGateway
{
    Task<Result<IReadOnlyList<Airport>>> GetAllAirportsAsync(
        CancellationToken cancellationToken = default);

    Task<Result<AirportFlightsResultDto>> GetFlightsAsync(
        int airportId,
        string? direction,
        bool live,
        CancellationToken cancellationToken = default);
}
