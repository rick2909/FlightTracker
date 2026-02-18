using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Implementation;

/// <summary>
/// Service for enriching and persisting airport data from AirportDB.
/// If an airport doesn't exist in the database, it fetches enrichment data
/// and creates a new airport record.
/// </summary>
public sealed class AirportEnrichmentService(
    IAirportLookupClient lookupClient,
    IAirportRepository repository,
    IMapper mapper) : IAirportEnrichmentService
{
    public async Task<AirportDto?> EnrichAirportAsync(
        string icaoCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(icaoCode))
            throw new ArgumentNullException(nameof(icaoCode), "ICAO code must be provided.");

        if (icaoCode.Length != 4)
            throw new ArgumentException("ICAO code must be 4 characters long.", nameof(icaoCode));

        var normalizedIcao = icaoCode.ToUpperInvariant();

        // Check if airport already exists in DB
        var existing = await repository.GetByCodeAsync(
            normalizedIcao,
            cancellationToken);

        if (existing is not null)
            return mapper.Map<AirportDto>(existing);

        // Fetch from external API
        var enrichmentData = await lookupClient.GetAirportAsync(
            normalizedIcao,
            cancellationToken);

        if (enrichmentData is null)
            return null;

        // Create new airport entity from enrichment data
        var newAirport = mapper.Map<Airport>(enrichmentData);

        // Persist to database
        var created = await repository.AddAsync(
            newAirport,
            cancellationToken);

        return mapper.Map<AirportDto>(created);
    }
}
