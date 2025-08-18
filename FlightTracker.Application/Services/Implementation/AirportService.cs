using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlightTracker.Application.Services.Implementation;

/// <summary>
/// Service implementation for airport-related business operations.
/// </summary>
public class AirportService : IAirportService
{
    private readonly IAirportRepository _airportRepository;
    private readonly ITimeApiService _timeApiService;

    public AirportService(IAirportRepository airportRepository, ITimeApiService timeApiService)
    {
        _airportRepository = airportRepository;
        _timeApiService = timeApiService;
    }

    public async Task<Airport?> GetAirportByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _airportRepository.GetByCodeAsync(code, cancellationToken);
    }

    public async Task<IReadOnlyList<Airport>> GetAllAirportsAsync(CancellationToken cancellationToken = default)
    {
        return await _airportRepository.GetAllAsync(cancellationToken);
    }

    public async Task<Airport> AddAirportAsync(Airport airport, CancellationToken cancellationToken = default)
    {
        return await _airportRepository.AddAsync(airport, cancellationToken);
    }

    public async Task UpdateAirportAsync(Airport airport, CancellationToken cancellationToken = default)
    {
        await _airportRepository.UpdateAsync(airport, cancellationToken);
    }

    public async Task DeleteAirportAsync(int id, CancellationToken cancellationToken = default)
    {
        await _airportRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<string?> GetTimeZoneIdByAirportCodeAsync(string airportCode, CancellationToken cancellationToken = default)
    {
        var airport = await _airportRepository.GetByCodeAsync(airportCode, cancellationToken);
        if (airport?.Latitude is double lat && airport.Longitude is double lon)
        {
            try
            {
                return await _timeApiService.GetTimeZoneIdAsync(lat, lon, cancellationToken);
            }
            catch
            {
                // Degrade gracefully: unavailable or timed out
                return null;
            }
        }
        return null;
    }
}
