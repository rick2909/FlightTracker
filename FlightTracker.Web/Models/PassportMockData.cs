using System;
using System.Collections.Generic;
using FlightTracker.Application.Dtos;

namespace YourApp.Models;

/// <summary>
/// Provides realistic mock data for the Passport screen.
/// </summary>
public static class PassportMockData
{
    public static PassportViewModel CreateSample()
    {
        // Example profile
        var vm = new PassportViewModel
        {
            UserName = "Alex Doe",
            AvatarUrl = "https://example.com/avatar.png",
            TotalFlights = 128,
            TotalMiles = 345_678,
            FavoriteAirline = "Delta",
            LongestFlightMiles = 8_765,
            ShortestFlightMiles = 123,
            AirlinesVisited = new() { "Delta", "KLM", "Air France", "British Airways", "JAL" },
            AirportsVisited = new() { "ATL", "JFK", "LAX", "SEA", "SFO", "AMS", "CDG", "LHR", "NRT" },
            CountriesVisitedIso2 = new() { "us", "nl", "fr", "gb", "jp", "de", "es" },
            FlightsPerYear = new()
            {
                { 2018, 3 },
                { 2019, 4 },
                { 2020, 1 },
                { 2021, 12 },
                { 2022, 18 },
                { 2023, 26 }
            },
            Routes = new()
            {
                // Example past intercontinental flight: JFK -> AMS
                new MapFlightDto
                {
                    FlightId = 1001,
                    FlightNumber = "DL48",
                    DepartureTimeUtc = new DateTime(2023, 05, 15, 22, 30, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2023, 05, 16, 06, 45, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "JFK",
                    ArrivalAirportCode = "AMS",
                    DepartureLat = 40.6413,
                    DepartureLon = -73.7781,
                    ArrivalLat = 52.3105,
                    ArrivalLon = 4.7683,
                    IsUpcoming = false
                },
                // Example upcoming domestic flight: SEA -> SFO
                new MapFlightDto
                {
                    FlightId = 1002,
                    FlightNumber = "DL1735",
                    DepartureTimeUtc = DateTime.UtcNow.AddDays(14),
                    ArrivalTimeUtc = DateTime.UtcNow.AddDays(14).AddHours(2),
                    DepartureAirportCode = "SEA",
                    ArrivalAirportCode = "SFO",
                    DepartureLat = 47.4502,
                    DepartureLon = -122.3088,
                    ArrivalLat = 37.6213,
                    ArrivalLon = -122.3790,
                    IsUpcoming = true
                },
                // Additional mock routes
                new MapFlightDto
                {
                    FlightId = 1003,
                    FlightNumber = "DL7",
                    DepartureTimeUtc = new DateTime(2022, 11, 10, 06, 15, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2022, 11, 10, 18, 45, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "LAX",
                    ArrivalAirportCode = "NRT",
                    DepartureLat = 33.9416,
                    DepartureLon = -118.4085,
                    ArrivalLat = 35.7720,
                    ArrivalLon = 140.3929,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1004,
                    FlightNumber = "KL1234",
                    DepartureTimeUtc = new DateTime(2021, 03, 02, 09, 00, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2021, 03, 02, 10, 30, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "AMS",
                    ArrivalAirportCode = "CDG",
                    DepartureLat = 52.3105,
                    DepartureLon = 4.7683,
                    ArrivalLat = 49.0097,
                    ArrivalLon = 2.5479,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1005,
                    FlightNumber = "AF106",
                    DepartureTimeUtc = new DateTime(2019, 07, 18, 14, 00, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2019, 07, 18, 14, 45, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "CDG",
                    ArrivalAirportCode = "LHR",
                    DepartureLat = 49.0097,
                    DepartureLon = 2.5479,
                    ArrivalLat = 51.4700,
                    ArrivalLon = -0.4543,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1006,
                    FlightNumber = "BA902",
                    DepartureTimeUtc = new DateTime(2018, 09, 05, 08, 25, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2018, 09, 05, 10, 45, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "LHR",
                    ArrivalAirportCode = "FRA",
                    DepartureLat = 51.4700,
                    DepartureLon = -0.4543,
                    ArrivalLat = 50.0379,
                    ArrivalLon = 8.5622,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1007,
                    FlightNumber = "LH96",
                    DepartureTimeUtc = new DateTime(2020, 02, 10, 12, 15, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2020, 02, 10, 13, 15, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "FRA",
                    ArrivalAirportCode = "MUC",
                    DepartureLat = 50.0379,
                    DepartureLon = 8.5622,
                    ArrivalLat = 48.3538,
                    ArrivalLon = 11.7861,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1008,
                    FlightNumber = "LH1802",
                    DepartureTimeUtc = new DateTime(2022, 04, 21, 09, 30, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2022, 04, 21, 11, 55, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "MUC",
                    ArrivalAirportCode = "MAD",
                    DepartureLat = 48.3538,
                    DepartureLon = 11.7861,
                    ArrivalLat = 40.4722,
                    ArrivalLon = -3.5608,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1009,
                    FlightNumber = "IB2711",
                    DepartureTimeUtc = new DateTime(2022, 05, 02, 15, 35, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2022, 05, 02, 16, 55, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "MAD",
                    ArrivalAirportCode = "BCN",
                    DepartureLat = 40.4722,
                    DepartureLon = -3.5608,
                    ArrivalLat = 41.2974,
                    ArrivalLon = 2.0833,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1010,
                    FlightNumber = "VY8450",
                    DepartureTimeUtc = new DateTime(2021, 10, 08, 10, 05, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2021, 10, 08, 11, 05, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "BCN",
                    ArrivalAirportCode = "LIS",
                    DepartureLat = 41.2974,
                    DepartureLon = 2.0833,
                    ArrivalLat = 38.7742,
                    ArrivalLon = -9.1342,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1011,
                    FlightNumber = "TP133",
                    DepartureTimeUtc = new DateTime(2019, 06, 14, 07, 20, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2019, 06, 14, 09, 35, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "LIS",
                    ArrivalAirportCode = "DUB",
                    DepartureLat = 38.7742,
                    DepartureLon = -9.1342,
                    ArrivalLat = 53.4213,
                    ArrivalLon = -6.2701,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1012,
                    FlightNumber = "DL45",
                    DepartureTimeUtc = new DateTime(2023, 09, 01, 12, 00, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2023, 09, 01, 15, 00, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "DUB",
                    ArrivalAirportCode = "JFK",
                    DepartureLat = 53.4213,
                    DepartureLon = -6.2701,
                    ArrivalLat = 40.6413,
                    ArrivalLon = -73.7781,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1013,
                    FlightNumber = "DL1228",
                    DepartureTimeUtc = new DateTime(2021, 12, 19, 17, 30, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2021, 12, 19, 20, 40, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "JFK",
                    ArrivalAirportCode = "MIA",
                    DepartureLat = 40.6413,
                    DepartureLon = -73.7781,
                    ArrivalLat = 25.7959,
                    ArrivalLon = -80.2870,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1014,
                    FlightNumber = "DL1279",
                    DepartureTimeUtc = new DateTime(2020, 01, 18, 14, 10, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2020, 01, 18, 16, 15, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "MIA",
                    ArrivalAirportCode = "ATL",
                    DepartureLat = 25.7959,
                    DepartureLon = -80.2870,
                    ArrivalLat = 33.6407,
                    ArrivalLon = -84.4277,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1015,
                    FlightNumber = "DL1083",
                    DepartureTimeUtc = new DateTime(2023, 02, 11, 11, 00, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2023, 02, 11, 12, 55, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "ATL",
                    ArrivalAirportCode = "DFW",
                    DepartureLat = 33.6407,
                    DepartureLon = -84.4277,
                    ArrivalLat = 32.8998,
                    ArrivalLon = -97.0403,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1016,
                    FlightNumber = "AA2470",
                    DepartureTimeUtc = new DateTime(2022, 08, 24, 16, 45, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2022, 08, 24, 17, 55, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "DFW",
                    ArrivalAirportCode = "DEN",
                    DepartureLat = 32.8998,
                    DepartureLon = -97.0403,
                    ArrivalLat = 39.8561,
                    ArrivalLon = -104.6737,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1017,
                    FlightNumber = "UA1543",
                    DepartureTimeUtc = new DateTime(2020, 10, 03, 19, 35, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2020, 10, 03, 21, 15, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "DEN",
                    ArrivalAirportCode = "SEA",
                    DepartureLat = 39.8561,
                    DepartureLon = -104.6737,
                    ArrivalLat = 47.4502,
                    ArrivalLon = -122.3088,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1018,
                    FlightNumber = "AS325",
                    DepartureTimeUtc = new DateTime(2018, 04, 11, 13, 20, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2018, 04, 11, 15, 45, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "SEA",
                    ArrivalAirportCode = "LAX",
                    DepartureLat = 47.4502,
                    DepartureLon = -122.3088,
                    ArrivalLat = 33.9416,
                    ArrivalLon = -118.4085,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1019,
                    FlightNumber = "UA837",
                    DepartureTimeUtc = DateTime.UtcNow.AddDays(20).Date.AddHours(17),
                    ArrivalTimeUtc = DateTime.UtcNow.AddDays(21).Date.AddHours(8),
                    DepartureAirportCode = "SFO",
                    ArrivalAirportCode = "HND",
                    DepartureLat = 37.6213,
                    DepartureLon = -122.3790,
                    ArrivalLat = 35.5494,
                    ArrivalLon = 139.7798,
                    IsUpcoming = true
                },
                new MapFlightDto
                {
                    FlightId = 1020,
                    FlightNumber = "JL92",
                    DepartureTimeUtc = new DateTime(2024, 03, 09, 03, 30, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2024, 03, 09, 05, 55, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "HND",
                    ArrivalAirportCode = "ICN",
                    DepartureLat = 35.5494,
                    DepartureLon = 139.7798,
                    ArrivalLat = 37.4602,
                    ArrivalLon = 126.4407,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1021,
                    FlightNumber = "KE641",
                    DepartureTimeUtc = new DateTime(2023, 06, 28, 00, 30, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2023, 06, 28, 08, 00, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "ICN",
                    ArrivalAirportCode = "SIN",
                    DepartureLat = 37.4602,
                    DepartureLon = 126.4407,
                    ArrivalLat = 1.3644,
                    ArrivalLon = 103.9915,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1022,
                    FlightNumber = "SQ494",
                    DepartureTimeUtc = new DateTime(2022, 02, 16, 02, 00, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2022, 02, 16, 06, 00, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "SIN",
                    ArrivalAirportCode = "DXB",
                    DepartureLat = 1.3644,
                    DepartureLon = 103.9915,
                    ArrivalLat = 25.2532,
                    ArrivalLon = 55.3657,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1023,
                    FlightNumber = "EK5",
                    DepartureTimeUtc = new DateTime(2021, 09, 07, 07, 10, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2021, 09, 07, 11, 30, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "DXB",
                    ArrivalAirportCode = "LHR",
                    DepartureLat = 25.2532,
                    DepartureLon = 55.3657,
                    ArrivalLat = 51.4700,
                    ArrivalLon = -0.4543,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1024,
                    FlightNumber = "QF400",
                    DepartureTimeUtc = new DateTime(2019, 05, 04, 21, 30, 00, DateTimeKind.Utc),
                    ArrivalTimeUtc = new DateTime(2019, 05, 04, 22, 45, 00, DateTimeKind.Utc),
                    DepartureAirportCode = "SYD",
                    ArrivalAirportCode = "MEL",
                    DepartureLat = -33.9399,
                    DepartureLon = 151.1753,
                    ArrivalLat = -37.6733,
                    ArrivalLon = 144.8433,
                    IsUpcoming = false
                },
                new MapFlightDto
                {
                    FlightId = 1025,
                    FlightNumber = "QF462",
                    DepartureTimeUtc = DateTime.UtcNow.AddDays(40).Date.AddHours(8),
                    ArrivalTimeUtc = DateTime.UtcNow.AddDays(40).Date.AddHours(9).AddMinutes(30),
                    DepartureAirportCode = "MEL",
                    ArrivalAirportCode = "SYD",
                    DepartureLat = -37.6733,
                    DepartureLon = 144.8433,
                    ArrivalLat = -33.9399,
                    ArrivalLon = 151.1753,
                    IsUpcoming = true
                }
            }
        };

        return vm;
    }
}
