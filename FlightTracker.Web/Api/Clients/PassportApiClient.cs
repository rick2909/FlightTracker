using FlightTracker.Application.Dtos;
using FlightTracker.Web.Api.Contracts;
using FlightTracker.Web.Services.Interfaces;

namespace FlightTracker.Web.Api.Clients;

public sealed class PassportApiClient(HttpClient httpClient)
    : IPassportApiClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<PassportDataDto?> GetPassportDataAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.ReadRequiredAsync<PassportDataResponse>(
            $"/api/v1/passport/users/{userId}",
            cancellationToken);

        if (response is null)
        {
            return null;
        }

        return new PassportDataDto
        {
            TotalFlights = response.TotalFlights,
            TotalMiles = response.TotalMiles,
            LongestFlightMiles = response.LongestFlightMiles,
            ShortestFlightMiles = response.ShortestFlightMiles,
            FavoriteAirline = response.FavoriteAirline,
            FavoriteAirport = response.FavoriteAirport,
            MostFlownAircraftType = response.MostFlownAircraftType,
            FavoriteClass = response.FavoriteClass,
            AirlinesVisited = response.AirlinesVisited,
            AirportsVisited = response.AirportsVisited,
            CountriesVisitedIso2 = response.CountriesVisitedIso2,
            FlightsPerYear = response.FlightsPerYear,
            FlightsByAirline = response.FlightsByAirline,
            FlightsByAircraftType = response.FlightsByAircraftType,
            Routes = response.Routes
        };
    }

    public async Task<PassportDetailsDto?> GetPassportDetailsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.ReadRequiredAsync<PassportDetailsResponse>(
            $"/api/v1/passport/users/{userId}/details",
            cancellationToken);

        if (response is null)
        {
            return null;
        }

        return new PassportDetailsDto
        {
            AirlineStats = response.AirlineStats
                .Select(stat => new AirlineStatsDto
                {
                    AirlineName = stat.AirlineName,
                    AirlineIata = stat.AirlineIata,
                    AirlineIcao = stat.AirlineIcao,
                    Count = stat.Count
                })
                .ToList(),
            AircraftTypeStats = response.AircraftTypeStats
        };
    }
}
