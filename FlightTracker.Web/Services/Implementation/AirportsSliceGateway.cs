using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;
using FlightTracker.Domain.Entities;
using FlightTracker.Web.Configuration;
using FlightTracker.Web.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FlightTracker.Web.Services.Implementation;

public sealed class AirportsSliceGateway(
    ILogger<AirportsSliceGateway> logger,
    IAirportsApiClient airportsApiClient,
    IOptions<FlightTrackerApiOptions> apiOptions) : IAirportsSliceGateway
{
    private readonly ILogger<AirportsSliceGateway> _logger = logger;
    private readonly IAirportsApiClient _airportsApiClient = airportsApiClient;
    private readonly FlightTrackerApiOptions _apiOptions = apiOptions.Value;

    public async Task<Result<IReadOnlyList<Airport>>> GetAllAirportsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!UseAirportsApi())
        {
            return Result<IReadOnlyList<Airport>>.Failure(
                "Airports API is not configured.",
                "airports.api.not_configured");
        }

        try
        {
            var airports = await _airportsApiClient.GetAirportsAsync(
                cancellationToken);

            return Result<IReadOnlyList<Airport>>.Success(airports);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading airports from API");
            return Result<IReadOnlyList<Airport>>.Failure(
                "Failed to load airports",
                "airports.api.load_failed");
        }
    }

    public async Task<Result<AirportFlightsResultDto>> GetFlightsAsync(
        int airportId,
        string? direction,
        bool live,
        CancellationToken cancellationToken = default)
    {
        if (!UseAirportsApi())
        {
            return Result<AirportFlightsResultDto>.Failure(
                "Airports API is not configured.",
                "airports.api.not_configured");
        }

        var airportsResult = await GetAllAirportsAsync(cancellationToken);

        if (airportsResult.IsFailure || airportsResult.Value is null)
        {
            return Result<AirportFlightsResultDto>.Failure(
                airportsResult.ErrorMessage ?? "Failed to load airports",
                airportsResult.ErrorCode ?? "airports.api.load_failed");
        }

        var airport = airportsResult.Value.FirstOrDefault(a => a.Id == airportId);

        if (airport is null)
        {
            return Result<AirportFlightsResultDto>.Failure(
                "Airport not found",
                "airport.not_found");
        }

        var code = airport.IataCode ?? airport.IcaoCode ?? airport.Name;

        try
        {
            var flights = await _airportsApiClient.GetFlightsAsync(
                code,
                direction,
                live,
                100,
                cancellationToken);

            return Result<AirportFlightsResultDto>.Success(flights);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading flights from API for airport {AirportId}", airportId);
            return Result<AirportFlightsResultDto>.Failure(
                "Failed to load flights",
                "airport_flights.api.load_failed");
        }
    }

    private bool UseAirportsApi()
    {
        return _apiOptions.Slices.Airports
            && Uri.TryCreate(_apiOptions.BaseUrl, UriKind.Absolute, out _);
    }
}
