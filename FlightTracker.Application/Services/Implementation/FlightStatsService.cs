using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Interfaces;

namespace FlightTracker.Application.Services.Implementation;

/// <summary>
/// Computes grouped flight statistics for a user.
/// </summary>
public class FlightStatsService : IFlightStatsService
{
    private readonly IUserFlightRepository _userFlightRepository;

    public FlightStatsService(IUserFlightRepository userFlightRepository)
    {
        _userFlightRepository = userFlightRepository;
    }

    public async Task<PassportDetailsDto> GetPassportDetailsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var flights = (await _userFlightRepository
            .GetUserFlightsAsync(userId, cancellationToken))
            .ToList();

        var flown = flights
            .Where(uf => uf.DidFly && uf.Flight != null)
            .ToList();

        var airlineStats = flown
            .Select(uf => uf.Flight!.OperatingAirline)
            .Where(a => a != null)
            .GroupBy(a =>
                $"{a!.Name?.Trim().ToUpperInvariant() ?? string.Empty}|{a.IataCode?.Trim().ToUpperInvariant() ?? string.Empty}|{a.IcaoCode?.Trim().ToUpperInvariant() ?? string.Empty}")
            .Select(g =>
            {
                var keyParts = g.Key.Split('|');
                return new AirlineStatsDto
                {
                    AirlineName = keyParts[0],
                    AirlineIata = string.IsNullOrEmpty(keyParts[1]) ? null : keyParts[1],
                    AirlineIcao = string.IsNullOrEmpty(keyParts[2]) ? null : keyParts[2],
                    Count = g.Count()
                };
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.AirlineName)
            .ToList();

        var aircraftTypeStats = flown
            .Select(uf => uf.Flight!.Aircraft)
            .Where(a => a != null)
            .Select(a => !string.IsNullOrWhiteSpace(a!.Model)
                ? a.Model!
                : (a.IcaoTypeCode ?? string.Empty))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .GroupBy(s => s!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Count(),
                StringComparer.OrdinalIgnoreCase);

        return new PassportDetailsDto
        {
            AirlineStats = airlineStats,
            AircraftTypeStats = aircraftTypeStats
        };
    }
}
