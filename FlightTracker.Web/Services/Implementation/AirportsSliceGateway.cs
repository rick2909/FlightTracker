using FlightTracker.Application.Dtos;
using FlightTracker.Application.Results;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;
using FlightTracker.Web.Configuration;
using FlightTracker.Web.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FlightTracker.Web.Services.Implementation;

public sealed class AirportsSliceGateway(
    ILogger<AirportsSliceGateway> logger,
    IAirportService airportService,
    IAirportOverviewService airportOverviewService,
    IAirportsApiClient airportsApiClient,
    IOptions<FlightTrackerApiOptions> apiOptions) : IAirportsSliceGateway
{
    private readonly ILogger<AirportsSliceGateway> _logger = logger;
    private readonly IAirportService _airportService = airportService;
    private readonly IAirportOverviewService _airportOverviewService = airportOverviewService;
    private readonly IAirportsApiClient _airportsApiClient = airportsApiClient;
    private readonly FlightTrackerApiOptions _apiOptions = apiOptions.Value;

    public async Task<Result<IReadOnlyList<Airport>>> GetAllAirportsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!UseAirportsApi())
        {
            return await _airportService.GetAllAirportsAsync(cancellationToken);
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
            return await GetFlightsFromApplicationAsync(
                airportId,
                direction,
                live,
                cancellationToken);
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

    private async Task<Result<AirportFlightsResultDto>> GetFlightsFromApplicationAsync(
        int airportId,
        string? direction,
        bool live,
        CancellationToken cancellationToken)
    {
        var airportsResult = await _airportService.GetAllAirportsAsync(
            cancellationToken);

        if (airportsResult.IsFailure || airportsResult.Value is null)
        {
            return Result<AirportFlightsResultDto>.Failure(
                airportsResult.ErrorMessage ?? "Failed to load airports",
                airportsResult.ErrorCode ?? "airports.load_failed");
        }

        var airport = airportsResult.Value.FirstOrDefault(a => a.Id == airportId);

        if (airport is null)
        {
            return Result<AirportFlightsResultDto>.Failure(
                "Airport not found",
                "airport.not_found");
        }

        var code = airport.IataCode ?? airport.IcaoCode ?? airport.Name;

        return await _airportOverviewService.GetFlightsAsync(
            code!,
            direction,
            live,
            100,
            cancellationToken);
    }

    private bool UseAirportsApi()
    {
        return _apiOptions.Slices.Airports
            && Uri.TryCreate(_apiOptions.BaseUrl, UriKind.Absolute, out _);
    }
}
