using AutoMapper;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Results;
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
    public async Task<Result<AirportDto>> EnrichAirportAsync(
        string icaoCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(icaoCode))
            throw new ArgumentNullException(nameof(icaoCode), "An ICAO code must be provided.");

        if (icaoCode.Length != 4)
            throw new ArgumentException("ICAO code must be 4 characters long.", nameof(icaoCode));

        var normalizedIcao = icaoCode.ToUpperInvariant();

        // Check if airport already exists in DB
        var existing = await repository.GetByCodeAsync(
            normalizedIcao,
            cancellationToken);

        if (existing is not null)
            return Result<AirportDto>.Success(mapper.Map<AirportDto>(existing));

        // Fetch from external API
        AirportEnrichmentDto? enrichmentData;
        try
        {
            enrichmentData = await lookupClient.GetAirportAsync(
                normalizedIcao,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<AirportDto>.Failure(
                ex.Message,
                "airport_enrichment.lookup_failed");
        }

        if (enrichmentData is null)
            return Result<AirportDto>.Success(null);

        // Create new airport entity from enrichment data
        var newAirport = mapper.Map<Airport>(enrichmentData);

        // Persist to database
        var created = await repository.AddAsync(
            newAirport,
            cancellationToken);

        return Result<AirportDto>.Success(mapper.Map<AirportDto>(created));
    }
}
