using System;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Resolves a Flight by flight number and date, first checking the local DB
/// and later (future) falling back to external providers.
/// </summary>
public interface IFlightLookupService
{
    Task<Result<Flight>> ResolveFlightAsync(string flightNumber, DateOnly date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns matching flights by flight number (partial/contains match); optional date narrows to that day.
    /// </summary>
    Task<Result<IReadOnlyList<Flight>>> SearchByFlightNumberAsync(string flightNumber, DateOnly? date = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns matching flights by route (departure/arrival airport code or city); optional date narrows to that day.
    /// </summary>
    Task<Result<IReadOnlyList<Flight>>> SearchByRouteAsync(string? departure, string? arrival, DateOnly? date = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves quick-add candidates for a flight number or callsign.
    /// Date is only applied to flights already present in the local database.
    /// </summary>
    Task<Result<IReadOnlyList<FlightLookupCandidateDto>>> SearchCandidatesByDesignatorAsync(
        string designator,
        DateOnly? date = null,
        CancellationToken cancellationToken = default);
}
