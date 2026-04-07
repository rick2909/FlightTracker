using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Results;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Implementation;

public class FlightLookupService(
    IFlightRepository flights,
    IFlightDataProvider provider,
    IFlightRouteLookupClient? flightRouteLookupClient = null,
    IAirportRepository? airportRepository = null,
    IAirlineRepository? airlineRepository = null) : IFlightLookupService
{
    private readonly IFlightRouteLookupClient? _flightRouteLookupClient = flightRouteLookupClient;
    private readonly IAirportRepository? _airportRepository = airportRepository;
    private readonly IAirlineRepository? _airlineRepository = airlineRepository;

    public async Task<Result<Flight>> ResolveFlightAsync(string flightNumber, DateOnly date, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(flightNumber))
            {
                return Result<Flight>.Success(null);
            }
            var normalized = flightNumber.Trim().ToUpperInvariant();
            var local = await flights.GetByFlightNumberAndDateAsync(normalized, date, cancellationToken);
            if (local != null)
            {
                return Result<Flight>.Success(local);
            }

            var startOfDayUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            Flight? candidate;
            try
            {
                candidate = await provider.GetFlightByNumberAsync(
                    normalized,
                    startOfDayUtc,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<Flight>.Failure(
                    ex.Message,
                    "flight_lookup.provider_failed");
            }

            return Result<Flight>.Success(candidate);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<Flight>.Failure(ex.Message, "flight_lookup.resolve.failed");
        }
    }

    public async Task<Result<IReadOnlyList<Flight>>> SearchByFlightNumberAsync(string flightNumber, DateOnly? date = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await flights.SearchByFlightNumberAsync(flightNumber, date, cancellationToken);
            return Result<IReadOnlyList<Flight>>.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<Flight>>.Failure(ex.Message, "flight_lookup.search_by_number.failed");
        }
    }

    public async Task<Result<IReadOnlyList<Flight>>> SearchByRouteAsync(string? departure, string? arrival, DateOnly? date = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await flights.SearchByRouteAsync(departure, arrival, date, cancellationToken);
            return Result<IReadOnlyList<Flight>>.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<Flight>>.Failure(ex.Message, "flight_lookup.search_by_route.failed");
        }
    }

    public async Task<Result<IReadOnlyList<FlightLookupCandidateDto>>> SearchCandidatesByDesignatorAsync(
        string designator,
        DateOnly? date = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(designator))
            {
                return Result<IReadOnlyList<FlightLookupCandidateDto>>.Success(
                    Array.Empty<FlightLookupCandidateDto>());
            }

            var normalized = NormalizeDesignator(designator);

            var localMatches = await flights.SearchByFlightNumberAsync(
                normalized,
                date,
                cancellationToken);

            // Keep only the latest departure per DB flight number to avoid duplicates.
            var candidates = localMatches
                .Where(f => !string.IsNullOrWhiteSpace(f.FlightNumber))
                .GroupBy(f => NormalizeDesignator(f.FlightNumber))
                .Select(group => group
                    .OrderByDescending(f => f.DepartureTimeUtc)
                    .First())
                .OrderByDescending(f => f.DepartureTimeUtc)
                .Select(MapDatabaseCandidate)
                .ToList();

            var externalCandidate = await BuildExternalCandidateAsync(
                normalized,
                cancellationToken);

            if (externalCandidate is not null)
            {
                var duplicateInDatabase = candidates.Any(candidate =>
                    candidate.IsFromDatabase
                    && string.Equals(
                        NormalizeDesignator(candidate.FlightNumber),
                        NormalizeDesignator(externalCandidate.FlightNumber),
                        StringComparison.Ordinal));

                if (!duplicateInDatabase)
                {
                    candidates.Insert(0, externalCandidate);
                }
            }

            return Result<IReadOnlyList<FlightLookupCandidateDto>>.Success(candidates);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<FlightLookupCandidateDto>>.Failure(
                ex.Message,
                "flight_lookup.search_candidates.failed");
        }
    }

    private static FlightLookupCandidateDto MapDatabaseCandidate(Flight flight)
    {
        return new FlightLookupCandidateDto(
            FlightId: flight.Id,
            FlightNumber: flight.FlightNumber,
            Callsign: flight.FlightNumber,
            DepartureTimeUtc: flight.DepartureTimeUtc,
            ArrivalTimeUtc: flight.ArrivalTimeUtc,
            DepartureCode: flight.DepartureAirport?.IataCode
                ?? flight.DepartureAirport?.IcaoCode,
            ArrivalCode: flight.ArrivalAirport?.IataCode
                ?? flight.ArrivalAirport?.IcaoCode,
            DepartureAirportName: flight.DepartureAirport?.Name,
            ArrivalAirportName: flight.ArrivalAirport?.Name,
            AirlineIataCode: flight.OperatingAirline?.IataCode,
            AirlineIcaoCode: flight.OperatingAirline?.IcaoCode,
            AirlineName: flight.OperatingAirline?.Name,
            IsFromDatabase: true);
    }

    private async Task<FlightLookupCandidateDto?> BuildExternalCandidateAsync(
        string designator,
        CancellationToken cancellationToken)
    {
        if (_flightRouteLookupClient is null)
        {
            return null;
        }

        var route = await ResolveRouteWithIcaoFallbackAsync(
            designator,
            cancellationToken);

        if (route is null)
        {
            return null;
        }

        var airline = route.Airline;

        return new FlightLookupCandidateDto(
            FlightId: null,
            FlightNumber: designator,
            Callsign: route.Callsign,
            DepartureTimeUtc: null,
            ArrivalTimeUtc: null,
            DepartureCode: route.Origin?.IataCode ?? route.Origin?.IcaoCode,
            ArrivalCode: route.Destination?.IataCode ?? route.Destination?.IcaoCode,
            DepartureAirportName: route.Origin?.Name,
            ArrivalAirportName: route.Destination?.Name,
            AirlineIataCode: airline?.Iata,
            AirlineIcaoCode: airline?.Icao,
            AirlineName: airline?.Name,
            IsFromDatabase: false);
    }

    private async Task<FlightRouteLookupResult?> ResolveRouteWithIcaoFallbackAsync(
        string designator,
        CancellationToken cancellationToken)
    {
        if (_flightRouteLookupClient is null)
        {
            return null;
        }

        var route = await _flightRouteLookupClient.GetFlightRouteAsync(
            designator,
            cancellationToken);

        if (route is not null)
        {
            return route;
        }

        var fallbackDesignator = await ConvertIataToIcaoDesignatorAsync(
            designator,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(fallbackDesignator)
            || string.Equals(fallbackDesignator, designator, StringComparison.Ordinal))
        {
            return null;
        }

        return await _flightRouteLookupClient.GetFlightRouteAsync(
            fallbackDesignator,
            cancellationToken);
    }

    private async Task<string?> ConvertIataToIcaoDesignatorAsync(
        string designator,
        CancellationToken cancellationToken)
    {
        if (_airlineRepository is null)
        {
            return null;
        }

        var trimmed = NormalizeDesignator(designator);
        if (trimmed.Length < 3)
        {
            return null;
        }

        var prefixLength = 0;
        while (prefixLength < trimmed.Length && char.IsLetter(trimmed[prefixLength]))
        {
            prefixLength++;
        }

        if (prefixLength != 2 || prefixLength >= trimmed.Length)
        {
            return null;
        }

        var iataPrefix = trimmed[..prefixLength];
        var suffix = trimmed[prefixLength..];
        var airline = await _airlineRepository.GetByIataAsync(
            iataPrefix,
            cancellationToken);

        if (airline is null || string.IsNullOrWhiteSpace(airline.IcaoCode))
        {
            return null;
        }

        return string.Concat(
            airline.IcaoCode.Trim().ToUpperInvariant(),
            suffix);
    }

    private static string NormalizeDesignator(string input)
    {
        return input.Trim().ToUpperInvariant();
    }
}
