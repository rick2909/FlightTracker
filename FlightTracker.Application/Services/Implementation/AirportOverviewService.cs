using System.Globalization;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Implementation;

public class AirportOverviewService(
    IFlightRepository flightRepository,
    IAirportLiveService airportLiveService) : IAirportOverviewService
{
    private readonly IFlightRepository _flightRepository = flightRepository;
    private readonly IAirportLiveService _airportLiveService = airportLiveService;

    public async Task<AirportFlightsResultDto> GetFlightsAsync(string code, string? dir, bool live, int limit, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new AirportFlightsResultDto();
        }

        if (limit <= 0) limit = 100;

        if (live)
        {
            return await BuildMergedLiveAsync(code, dir, limit, cancellationToken);
        }

        // DB-only
        var allDeparting = await _flightRepository.SearchByRouteAsync(code, null, null, cancellationToken);
        var allArriving = await _flightRepository.SearchByRouteAsync(null, code, null, cancellationToken);

        IEnumerable<AirportFlightListItemDto> departing = allDeparting
            .OrderByDescending(f => f.DepartureTimeUtc)
            .Take(limit)
            .Select(MapDbFlight);
        IEnumerable<AirportFlightListItemDto> arriving = allArriving
            .OrderByDescending(f => f.DepartureTimeUtc)
            .Take(limit)
            .Select(MapDbFlight);

        return dir?.ToLowerInvariant() switch
        {
            "departing" => new AirportFlightsResultDto { Departing = departing },
            "arriving" => new AirportFlightsResultDto { Arriving = arriving },
            _ => new AirportFlightsResultDto { Departing = departing, Arriving = arriving }
        };
    }

    private async Task<AirportFlightsResultDto> BuildMergedLiveAsync(string code, string? dir, int limit, CancellationToken ct)
    {
        var dbDepTask = _flightRepository.SearchByRouteAsync(code, null, null, ct);
        var dbArrTask = _flightRepository.SearchByRouteAsync(null, code, null, ct);
        var liveDepTask = _airportLiveService.GetDeparturesAsync(code, limit, ct);
        var liveArrTask = _airportLiveService.GetArrivalsAsync(code, limit, ct);

        await Task.WhenAll(dbDepTask, dbArrTask, liveDepTask, liveArrTask);

        if (string.Equals(dir, "departing", StringComparison.OrdinalIgnoreCase))
        {
            var departing = BuildMerged(dbDepTask.Result, liveDepTask.Result, limit);
            return new AirportFlightsResultDto { Departing = departing };
        }
        if (string.Equals(dir, "arriving", StringComparison.OrdinalIgnoreCase))
        {
            var arriving = BuildMerged(dbArrTask.Result, liveArrTask.Result, limit);
            return new AirportFlightsResultDto { Arriving = arriving };
        }

        var mergedDeparting = BuildMerged(dbDepTask.Result, liveDepTask.Result, limit);
        var mergedArriving = BuildMerged(dbArrTask.Result, liveArrTask.Result, limit);
        return new AirportFlightsResultDto { Departing = mergedDeparting, Arriving = mergedArriving };
    }

    private static IEnumerable<AirportFlightListItemDto> BuildMerged(IEnumerable<Flight> db, IEnumerable<LiveFlightDto> live, int limit)
    {
        var dict = new Dictionary<string, AirportFlightListItemDto>();
        foreach (var f in db)
        {
            var item = MapDbFlight(f);
            dict[DedupKey(item)] = item; // DB primary
        }
        foreach (var l in live)
        {
            var item = MapLiveFlight(l);
            var key = DedupKey(item);
            if (dict.TryGetValue(key, out var existing))
            {
                dict[key] = Combine(existing, item);
            }
            else
            {
                dict[key] = item;
            }
        }

        return dict.Values
            .OrderByDescending(i => i.DepartureTimeUtc)
            .Take(limit);
    }

    private static string DedupKey(AirportFlightListItemDto i)
        => $"{i.FlightNumber?.Trim().ToUpperInvariant()}|{i.DepartureCode?.Trim().ToUpperInvariant()}→{i.ArrivalCode?.Trim().ToUpperInvariant()}";

    private static AirportFlightListItemDto Combine(AirportFlightListItemDto primary, AirportFlightListItemDto secondary)
        => new()
        {
            Id = primary.Id ?? secondary.Id,
            FlightNumber = string.IsNullOrWhiteSpace(primary.FlightNumber) ? secondary.FlightNumber : primary.FlightNumber,
            Airline = string.IsNullOrWhiteSpace(primary.Airline) ? secondary.Airline : primary.Airline,
            Aircraft = string.IsNullOrWhiteSpace(primary.Aircraft) ? secondary.Aircraft : primary.Aircraft,
            DepartureTimeUtc = string.IsNullOrWhiteSpace(primary.DepartureTimeUtc) ? secondary.DepartureTimeUtc : primary.DepartureTimeUtc,
            ArrivalTimeUtc = string.IsNullOrWhiteSpace(primary.ArrivalTimeUtc) ? secondary.ArrivalTimeUtc : primary.ArrivalTimeUtc,
            DepartureCode = string.IsNullOrWhiteSpace(primary.DepartureCode) ? secondary.DepartureCode : primary.DepartureCode,
            ArrivalCode = string.IsNullOrWhiteSpace(primary.ArrivalCode) ? secondary.ArrivalCode : primary.ArrivalCode
        };

    private static AirportFlightListItemDto MapDbFlight(Flight f)
        => new()
        {
            Id = f.Id,
            FlightNumber = f.FlightNumber ?? string.Empty,
            Airline = f.OperatingAirline?.Name,
            Aircraft = f.Aircraft != null
                ? (string.IsNullOrWhiteSpace(f.Aircraft.Registration) ? f.Aircraft.Model : $"{f.Aircraft.Model} • {f.Aircraft.Registration}")
                : null,
            DepartureTimeUtc = f.DepartureTimeUtc.ToString("o", CultureInfo.InvariantCulture),
            ArrivalTimeUtc = f.ArrivalTimeUtc.ToString("o", CultureInfo.InvariantCulture),
            DepartureCode = f.DepartureAirport is null ? null : (f.DepartureAirport.IataCode ?? f.DepartureAirport.IcaoCode ?? f.DepartureAirport.Name),
            ArrivalCode = f.ArrivalAirport is null ? null : (f.ArrivalAirport.IataCode ?? f.ArrivalAirport.IcaoCode ?? f.ArrivalAirport.Name)
        };

    private static AirportFlightListItemDto MapLiveFlight(LiveFlightDto l)
        => new()
        {
            Id = null,
            FlightNumber = l.FlightNumber,
            Airline = l.AirlineName ?? l.AirlineIata ?? l.AirlineIcao,
            Aircraft = !string.IsNullOrWhiteSpace(l.AircraftRegistration)
                ? (string.IsNullOrWhiteSpace(l.AircraftIcaoType) ? l.AircraftRegistration : $"{l.AircraftIcaoType} • {l.AircraftRegistration}")
                : l.AircraftIcaoType,
            DepartureTimeUtc = (l.DepartureActualUtc ?? l.DepartureScheduledUtc)?.ToString("o", CultureInfo.InvariantCulture),
            ArrivalTimeUtc = (l.ArrivalActualUtc ?? l.ArrivalScheduledUtc)?.ToString("o", CultureInfo.InvariantCulture),
            DepartureCode = l.DepartureIata ?? l.DepartureIcao,
            ArrivalCode = l.ArrivalIata ?? l.ArrivalIcao
        };
}
