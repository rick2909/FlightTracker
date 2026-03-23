using FlightTracker.Application.Dtos;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Web.Services.Interfaces;

public interface IAirportsApiClient
{
    Task<IReadOnlyList<Airport>> GetAirportsAsync(
        CancellationToken cancellationToken = default);

    Task<AirportFlightsResultDto> GetFlightsAsync(
        string code,
        string? direction,
        bool live,
        int limit,
        CancellationToken cancellationToken = default);
}
