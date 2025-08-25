using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Application.Services.Interfaces.Analytics;
using FlightTracker.Domain.Enums;

namespace FlightTracker.Application.Services.Implementation;

/// <summary>
/// Aggregates user flight data into PassportDataDto.
/// </summary>
public class PassportService : IPassportService
{
    private readonly IUserFlightRepository _userFlightRepository;
    private readonly IFlightRepository _flightRepository;
    private readonly IMapFlightService _mapFlightService;
    private readonly IDistanceCalculator _distanceCalculator;

    public PassportService(
        IUserFlightRepository userFlightRepository,
        IFlightRepository flightRepository,
        IMapFlightService mapFlightService,
        IDistanceCalculator distanceCalculator)
    {
        _userFlightRepository = userFlightRepository;
        _flightRepository = flightRepository;
        _mapFlightService = mapFlightService;
        _distanceCalculator = distanceCalculator;
    }

    public async Task<PassportDataDto> GetPassportDataAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userFlights = (await _userFlightRepository.GetUserFlightsAsync(userId, cancellationToken)).ToList();

        // Only consider flights the user actually flew for most stats
        var flown = userFlights.Where(uf => uf.DidFly && uf.Flight != null).ToList();

        int TotalFlights() => flown.Count;

        // Airlines visited by OperatingAirline name if available
        var airlinesVisited = flown
            .Select(uf => uf.Flight!.OperatingAirline?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();

        // Airports visited by code with preference for IATA
        var airportsVisited = flown
            .SelectMany(uf => new[]
            {
                uf.Flight!.DepartureAirport?.IataCode ?? uf.Flight!.DepartureAirport?.IcaoCode,
                uf.Flight!.ArrivalAirport?.IataCode ?? uf.Flight!.ArrivalAirport?.IcaoCode
            })
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();

        // Countries visited (we only have Country names in seed; map to upper ISO2 if already in ISO2)
        var countriesVisited = flown
            .SelectMany(uf => new[] { uf.Flight!.DepartureAirport?.Country, uf.Flight!.ArrivalAirport?.Country })
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(c => c.Length == 2 ? c.ToLowerInvariant() : c) // keep names if not ISO2
            .OrderBy(c => c)
            .ToList();

        // Distance per flight and extremes
        var distanceMiles = new List<(int flightId, int miles)>();
        foreach (var uf in flown)
        {
            var dep = uf.Flight!.DepartureAirport;
            var arr = uf.Flight!.ArrivalAirport;
            var km = (dep != null && arr != null) ? _distanceCalculator.CalculateGreatCircleKm(dep, arr) : null;
            if (km.HasValue)
            {
                var miles = (int)Math.Round(km.Value * 0.621371);
                distanceMiles.Add((uf.FlightId, miles));
            }
        }

        var totalMiles = distanceMiles.Sum(x => x.miles);
        var longestMiles = distanceMiles.Count > 0 ? distanceMiles.Max(x => x.miles) : 0;
        var shortestMiles = distanceMiles.Count > 0 ? distanceMiles.Min(x => x.miles) : 0;

        // Favorite airline/airport by frequency
        string favoriteAirline = airlinesVisited
            .OrderByDescending(a => flown.Count(uf => string.Equals(uf.Flight!.OperatingAirline?.Name, a, StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault() ?? string.Empty;

        string favoriteAirport = airportsVisited
            .OrderByDescending(ac => flown.Count(uf => string.Equals(uf.Flight!.DepartureAirport?.IataCode ?? uf.Flight!.DepartureAirport?.IcaoCode, ac, StringComparison.OrdinalIgnoreCase)
                                                    || string.Equals(uf.Flight!.ArrivalAirport?.IataCode ?? uf.Flight!.ArrivalAirport?.IcaoCode, ac, StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault() ?? string.Empty;

        // Most flown aircraft type (Manufacturer + Model or Model)
        var aircraftTypes = flown
            .Select(uf => uf.Flight!.Aircraft)
            .Where(a => a != null)
            .Select(a => string.IsNullOrWhiteSpace(a!.Model) ? a!.IcaoTypeCode ?? string.Empty : a!.Model)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        string mostFlownAircraftType = aircraftTypes
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? string.Empty;

        // Favorite class by count
        string favoriteClass = Enum.GetValues(typeof(FlightClass))
            .Cast<FlightClass>()
            .OrderByDescending(fc => flown.Count(uf => uf.FlightClass == fc))
            .Select(fc => fc.ToString())
            .FirstOrDefault() ?? string.Empty;

        // Flights per year (by departure year)
        var flightsPerYear = flown
            .GroupBy(uf => uf.Flight!.DepartureTimeUtc.Year)
            .ToDictionary(g => g.Key, g => g.Count());

        // Breakdown: flights by airline (OperatingAirline.Name)
        var flightsByAirline = flown
            .Select(uf => uf.Flight!.OperatingAirline?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        // Breakdown: flights by aircraft type (Model or ICAO type)
        var flightsByAircraftType = flown
            .Select(uf => uf.Flight!.Aircraft)
            .Where(a => a != null)
            .Select(a => string.IsNullOrWhiteSpace(a!.Model) ? a!.IcaoTypeCode ?? string.Empty : a!.Model)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .GroupBy(s => s!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        // Map routes (reuse existing service, include upcoming too)
        var routes = (await _mapFlightService.GetUserMapFlightsAsync(userId, maxPast: 1000, maxUpcoming: 1000, cancellationToken)).ToList();

        return new PassportDataDto
        {
            TotalFlights = TotalFlights(),
            TotalMiles = totalMiles,
            LongestFlightMiles = longestMiles,
            ShortestFlightMiles = shortestMiles,
            FavoriteAirline = favoriteAirline,
            FavoriteAirport = favoriteAirport,
            MostFlownAircraftType = mostFlownAircraftType,
            FavoriteClass = favoriteClass,
            AirlinesVisited = airlinesVisited,
            AirportsVisited = airportsVisited,
            CountriesVisitedIso2 = countriesVisited,
            FlightsPerYear = flightsPerYear,
            FlightsByAirline = flightsByAirline,
            FlightsByAircraftType = flightsByAircraftType,
            Routes = routes
        };
    }
}
