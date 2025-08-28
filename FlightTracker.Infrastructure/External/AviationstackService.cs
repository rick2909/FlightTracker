using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FlightTracker.Infrastructure.External;

/// <summary>
/// Aviationstack implementation for live airport departures/arrivals.
/// </summary>
public class AviationstackService : IAirportLiveService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public AviationstackService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Aviationstack:ApiKey"] ?? string.Empty;
    }

    public Task<IReadOnlyList<LiveFlightDto>> GetDeparturesAsync(string airportCode, int limit = 50, CancellationToken cancellationToken = default)
        => QueryAsync(depIata: airportCode, arrIata: null, limit: limit, cancellationToken);

    public Task<IReadOnlyList<LiveFlightDto>> GetArrivalsAsync(string airportCode, int limit = 50, CancellationToken cancellationToken = default)
        => QueryAsync(depIata: null, arrIata: airportCode, limit: limit, cancellationToken);

    private async Task<IReadOnlyList<LiveFlightDto>> QueryAsync(string? depIata, string? arrIata, int limit, CancellationToken cancellationToken)
    {
        var url = $"https://api.aviationstack.com/v1/flights?access_key={Uri.EscapeDataString(_apiKey)}&limit={limit}";
        if (!string.IsNullOrWhiteSpace(depIata)) url += $"&dep_iata={Uri.EscapeDataString(depIata!)}";
        if (!string.IsNullOrWhiteSpace(arrIata)) url += $"&arr_iata={Uri.EscapeDataString(arrIata!)}";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);

        var json = await JsonSerializer.DeserializeAsync<AviationstackResponse>(stream, s_Json, cancellationToken) 
                   ?? new AviationstackResponse();

        var list = new List<LiveFlightDto>(json.Data?.Count ?? 0);
        if (json.Data != null)
        {
            foreach (var f in json.Data)
            {
                list.Add(new LiveFlightDto
                {
                    FlightNumber = f.Flight?.Iata ?? f.Flight?.Icao ?? f.Flight?.Number ?? string.Empty,
                    Status = MapStatus(f.FlightStatus),
                    AirlineName = f.Airline?.Name,
                    AirlineIata = f.Airline?.Iata,
                    AirlineIcao = f.Airline?.Icao,
                    DepartureIata = f.Departure?.Iata,
                    DepartureIcao = f.Departure?.Icao,
                    DepartureScheduledUtc = ParseUtc(f.Departure?.Scheduled),
                    DepartureActualUtc = ParseUtc(f.Departure?.Actual),
                    ArrivalIata = f.Arrival?.Iata,
                    ArrivalIcao = f.Arrival?.Icao,
                    ArrivalScheduledUtc = ParseUtc(f.Arrival?.Scheduled),
                    ArrivalActualUtc = ParseUtc(f.Arrival?.Actual),
                    AircraftRegistration = f.Aircraft?.Registration,
                    AircraftIcaoType = f.Aircraft?.Icao
                });
            }
        }

        return list;
    }

    private static DateTime? ParseUtc(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
        {
            return dt.ToUniversalTime();
        }
        return null;
    }

    private static Domain.Enums.FlightStatus? MapStatus(string? apiStatus) => apiStatus?.ToLowerInvariant() switch
    {
        "scheduled" => Domain.Enums.FlightStatus.Scheduled,
        "active" => Domain.Enums.FlightStatus.InFlight,
        "landed" => Domain.Enums.FlightStatus.Landed,
        "cancelled" => Domain.Enums.FlightStatus.Cancelled,
        "diverted" => Domain.Enums.FlightStatus.Diverted,
        _ => null
    };

    private static readonly JsonSerializerOptions s_Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private sealed class AviationstackResponse
    {
        [JsonPropertyName("data")] public List<FlightItem>? Data { get; set; }
    }

    private sealed class FlightItem
    {
        [JsonPropertyName("flight_date")] public string? FlightDate { get; set; }
        [JsonPropertyName("flight_status")] public string? FlightStatus { get; set; }
        [JsonPropertyName("airline")] public AirlineItem? Airline { get; set; }
        [JsonPropertyName("flight")] public FlightCodeItem? Flight { get; set; }
        [JsonPropertyName("departure")] public DepArrItem? Departure { get; set; }
        [JsonPropertyName("arrival")] public DepArrItem? Arrival { get; set; }
        [JsonPropertyName("aircraft")] public AircraftItem? Aircraft { get; set; }
    }

    private sealed class AirlineItem { public string? Name { get; set; } public string? Iata { get; set; } public string? Icao { get; set; } }
    private sealed class FlightCodeItem { public string? Number { get; set; } public string? Iata { get; set; } public string? Icao { get; set; } }
    private sealed class DepArrItem { public string? Iata { get; set; } public string? Icao { get; set; } public string? Scheduled { get; set; } public string? Actual { get; set; } }
    private sealed class AircraftItem { [JsonPropertyName("registration")] public string? Registration { get; set; } [JsonPropertyName("icao")] public string? Icao { get; set; } }
}
