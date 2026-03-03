using System;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Results;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Implementation;

public class FlightLookupService(IFlightRepository flights, IFlightDataProvider provider) : IFlightLookupService
{
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
            var candidateResult = await provider.GetFlightByNumberAsync(
                normalized,
                startOfDayUtc,
                cancellationToken);

            if (candidateResult.IsFailure)
            {
                return Result<Flight>.Failure(
                    candidateResult.ErrorMessage ?? "External provider lookup failed.",
                    candidateResult.ErrorCode ?? "flight_lookup.provider_failed");
            }

            return Result<Flight>.Success(candidateResult.Value);
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
}
