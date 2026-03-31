using System.Net.Http.Json;
using FlightTracker.Application.Dtos;
using FlightTracker.Web.Api.Contracts;
using FlightTracker.Web.Services.Interfaces;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Web.Api.Clients;

public sealed class AirportsApiClient(HttpClient httpClient)
    : IAirportsApiClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<IReadOnlyList<Airport>> GetAirportsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<List<AirportApiResponse>>(
            "/api/v1/airports",
            cancellationToken);

        return response?.Select(MapAirport).ToList()
            ?? new List<Airport>();
    }

    public async Task<AirportFlightsResultDto> GetFlightsAsync(
        string code,
        string? direction,
        bool live,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = $"live={live.ToString().ToLowerInvariant()}&limit={limit}";

        if (!string.IsNullOrWhiteSpace(direction))
        {
            query += $"&direction={Uri.EscapeDataString(direction)}";
        }

        var endpoint = $"/api/v1/airports/{Uri.EscapeDataString(code)}/flights?{query}";
        var response = await _httpClient.GetFromJsonAsync<AirportFlightsApiResponse>(
            endpoint,
            cancellationToken);

        return new AirportFlightsResultDto
        {
            Departing = response?.Departing.Select(MapFlight)
                .ToList()
                ?? new List<AirportFlightListItemDto>(),
            Arriving = response?.Arriving.Select(MapFlight)
                .ToList()
                ?? new List<AirportFlightListItemDto>()
        };
    }

    private static Airport MapAirport(AirportApiResponse airport)
    {
        return new Airport
        {
            Id = airport.Id,
            Name = airport.Name,
            City = airport.City,
            Country = airport.Country,
            IataCode = airport.IataCode,
            IcaoCode = airport.IcaoCode,
            Latitude = airport.Latitude,
            Longitude = airport.Longitude,
            TimeZoneId = airport.TimeZoneId
        };
    }

    private static AirportFlightListItemDto MapFlight(
        AirportFlightListItemApiResponse flight)
    {
        return new AirportFlightListItemDto
        {
            Id = flight.Id,
            FlightNumber = flight.FlightNumber,
            Airline = flight.Airline,
            Aircraft = flight.Aircraft,
            DepartureTimeUtc = flight.DepartureTimeUtc,
            ArrivalTimeUtc = flight.ArrivalTimeUtc,
            DepartureCode = flight.DepartureCode,
            ArrivalCode = flight.ArrivalCode
        };
    }
}
