using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;
using FlightTracker.Domain.Enums;
using FlightTracker.Web.Api.Clients;
using Xunit;

namespace FlightTracker.Web.Tests;

public class ApiClientTests
{
    [Fact]
    public async Task UserFlightsApiClient_GetsFlightsList()
    {
        using var httpClient = CreateHttpClient("""
            [
              {
                "id": 4,
                "userId": 7,
                "flightId": 12,
                "flightClass": 0,
                "seatNumber": "12A",
                "bookedOnUtc": "2026-03-01T10:00:00Z",
                "didFly": true,
                "flightNumber": "FT100",
                "flightStatus": 0,
                "departureTimeUtc": "2026-03-10T10:00:00Z",
                "arrivalTimeUtc": "2026-03-10T12:00:00Z",
                "departureAirportCode": "AMS",
                "departureAirportName": "Amsterdam Airport Schiphol",
                "departureCity": "Amsterdam",
                "arrivalAirportCode": "LHR",
                "arrivalAirportName": "London Heathrow",
                "arrivalCity": "London"
              }
            ]
            """);
        var client = new UserFlightsApiClient(httpClient);

        var flights = await client.GetUserFlightsAsync(7, CancellationToken.None);

        var flight = Assert.Single(flights);
        Assert.Equal("FT100", flight.FlightNumber);
        Assert.Equal("AMS", flight.DepartureAirportCode);
    }

    [Fact]
    public async Task PassportApiClient_MapsPassportDetails()
    {
        using var httpClient = CreateHttpClient("""
            {
              "airlineStats": [
                {
                  "airlineName": "KLM",
                  "airlineIata": "KL",
                  "airlineIcao": "KLM",
                  "count": 3
                }
              ],
              "aircraftTypeStats": {
                "B738": 2,
                "A320": 1
              }
            }
            """);
        var client = new PassportApiClient(httpClient);

        var details = await client.GetPassportDetailsAsync(42, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Single(details!.AirlineStats);
        Assert.Equal("KLM", details.AirlineStats[0].AirlineName);
        Assert.Equal(2, details.AircraftTypeStats["B738"]);
    }

    [Fact]
    public async Task UserPreferencesApiClient_MapsPreferencesPayload()
    {
        using var httpClient = CreateHttpClient("""
            {
              "id": 5,
              "userId": 9,
              "distanceUnit": 1,
              "temperatureUnit": 1,
              "timeFormat": 1,
              "dateFormat": 2,
              "profileVisibility": 1,
              "showTotalMiles": true,
              "showAirlines": false,
              "showCountries": true,
              "showMapRoutes": false,
              "enableActivityFeed": true,
              "createdAtUtc": "2026-03-01T10:00:00Z",
              "updatedAtUtc": "2026-03-05T10:00:00Z"
            }
            """);
        var client = new UserPreferencesApiClient(httpClient);

        var dto = await client.GetAsync(9, CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(DistanceUnit.Kilometers, dto!.DistanceUnit);
        Assert.False(dto.ShowAirlines);
        Assert.True(dto.EnableActivityFeed);
    }

    [Fact]
    public async Task UserFlightsApiClient_ThrowsProblemDetailsMessage_OnNonSuccess()
    {
        using var httpClient = CreateHttpClient(
            "{\"detail\":\"Bad request from API\"}",
            HttpStatusCode.BadRequest);
        var client = new UserFlightsApiClient(httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetUserFlightsAsync(7, CancellationToken.None));

        Assert.Equal("Bad request from API", exception.Message);
    }

    private static HttpClient CreateHttpClient(
        string responseJson,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    responseJson,
                    Encoding.UTF8,
                    "application/json")
            });

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7001")
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory = responseFactory;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
