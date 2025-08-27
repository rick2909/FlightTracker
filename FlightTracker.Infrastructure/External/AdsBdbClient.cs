using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Services.Interfaces;

namespace FlightTracker.Infrastructure.External;

public sealed class AdsBdbClient(HttpClient http) : IFlightRouteLookupClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<FlightRouteLookupResult?> GetFlightRouteAsync(string callsign, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(callsign)) return null;
        var url = $"https://api.adsbdb.com/v0/callsign/{Uri.EscapeDataString(callsign)}";
        using var res = await http.GetAsync(url, cancellationToken);
        if (!res.IsSuccessStatusCode) return null;
        await using var stream = await res.Content.ReadAsStreamAsync(cancellationToken);
        var root = await JsonSerializer.DeserializeAsync<Root>(stream, JsonOptions, cancellationToken);
        var fr = root?.Response?.FlightRoute;
        if (fr is null) return null;

        FlightRouteAirline? al = fr.Airline is null ? null : new(
            Name: fr.Airline.Name,
            Icao: fr.Airline.Icao,
            Iata: fr.Airline.Iata,
            CountryName: fr.Airline.Country,
            CountryIso: fr.Airline.CountryIso,
            Callsign: fr.Airline.Callsign
        );

        static FlightRouteAirport MapAirport(AirportJson a) => new(
            IataCode: a.IataCode,
            IcaoCode: a.IcaoCode,
            Name: a.Name,
            Municipality: a.Municipality,
            CountryName: a.CountryName,
            CountryIsoName: a.CountryIsoName,
            Latitude: a.Latitude,
            Longitude: a.Longitude,
            ElevationFeet: a.Elevation
        );

        var origin = fr.Origin is null ? null : MapAirport(fr.Origin);
        var dest = fr.Destination is null ? null : MapAirport(fr.Destination);

        return new FlightRouteLookupResult(
            Callsign: fr.Callsign ?? callsign,
            Airline: al,
            Origin: origin,
            Destination: dest
        );
    }

    private sealed class Root
    {
        [JsonPropertyName("response")] public ResponseJson? Response { get; set; }
    }
    private sealed class ResponseJson
    {
        [JsonPropertyName("flightroute")] public FlightRouteJson? FlightRoute { get; set; }
    }
    private sealed class FlightRouteJson
    {
        [JsonPropertyName("callsign")] public string? Callsign { get; set; }
        [JsonPropertyName("airline")] public AirlineJson? Airline { get; set; }
        [JsonPropertyName("origin")] public AirportJson? Origin { get; set; }
        [JsonPropertyName("destination")] public AirportJson? Destination { get; set; }
    }
    private sealed class AirlineJson
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("icao")] public string? Icao { get; set; }
        [JsonPropertyName("iata")] public string? Iata { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
        [JsonPropertyName("country_iso")] public string? CountryIso { get; set; }
        [JsonPropertyName("callsign")] public string? Callsign { get; set; }
    }
    private sealed class AirportJson
    {
        [JsonPropertyName("iata_code")] public string? IataCode { get; set; }
        [JsonPropertyName("icao_code")] public string? IcaoCode { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("municipality")] public string? Municipality { get; set; }
        [JsonPropertyName("country_name")] public string? CountryName { get; set; }
        [JsonPropertyName("country_iso_name")] public string? CountryIsoName { get; set; }
        [JsonPropertyName("latitude")] public double? Latitude { get; set; }
        [JsonPropertyName("longitude")] public double? Longitude { get; set; }
        [JsonPropertyName("elevation")] public int? Elevation { get; set; }
    }
}
