using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Application.Dtos;
using Microsoft.Extensions.Configuration;

namespace FlightTracker.Infrastructure.External;

/// <summary>
/// Client for AirportDB.io API to fetch airport information by ICAO code.
/// Slowly populates database with new airports as they are discovered.
/// </summary>
public sealed class AirportDbClient(HttpClient http, IConfiguration configuration) : IAirportLookupClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<AirportEnrichmentDto?> GetAirportAsync(
        string icaoCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(icaoCode))
            return null;

        var apiToken = configuration["ApiKeys:AirportDB"];
        if (string.IsNullOrWhiteSpace(apiToken))
            return null; // API token not configured; skip enrichment

        var url =
            $"https://airportdb.io/api/v1/airport/{Uri.EscapeDataString(icaoCode)}?apiToken={Uri.EscapeDataString(apiToken)}";

        try
        {
            using var res = await http.GetAsync(url, cancellationToken);
            if (!res.IsSuccessStatusCode)
                return null;

            await using var stream = await res.Content.ReadAsStreamAsync(cancellationToken);
            var airport = await JsonSerializer.DeserializeAsync<AirportDbResponse>(
                stream,
                JsonOptions,
                cancellationToken);

            if (airport is null)
                return null;

            return new AirportEnrichmentDto(
                IataCode: airport.IataCode,
                IcaoCode: airport.IcaoCode,
                Name: airport.Name,
                Municipality: airport.Municipality,
                CountryName: airport.IsoCountry,
                CountryIsoCode: airport.IsoCountry,
                Latitude: airport.LatitudeDeg,
                Longitude: airport.LongitudeDeg,
                ElevationFeet: airport.ElevationFt,
                AirportType: airport.Type
            );
        }
        catch
        {
            // Log error if needed; fail gracefully
            return null;
        }
    }

    private sealed class AirportDbResponse
    {
        [JsonPropertyName("ident")]
        public string? Ident { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("latitude_deg")]
        public double? LatitudeDeg { get; set; }

        [JsonPropertyName("longitude_deg")]
        public double? LongitudeDeg { get; set; }

        [JsonPropertyName("elevation_ft")]
        public int? ElevationFt { get; set; }

        [JsonPropertyName("iso_country")]
        public string? IsoCountry { get; set; }

        [JsonPropertyName("municipality")]
        public string? Municipality { get; set; }

        [JsonPropertyName("iata_code")]
        public string? IataCode { get; set; }

        [JsonPropertyName("icao_code")]
        public string? IcaoCode { get; set; }
    }
}
