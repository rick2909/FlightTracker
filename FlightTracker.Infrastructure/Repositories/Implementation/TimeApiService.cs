using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Services.Interfaces;

namespace FlightTracker.Infrastructure.Repositories.Implementation;

/// <summary>
/// Calls https://timeapi.io/api/timezone/coordinate to get IANA time zone by lat/lon.
/// </summary>
public class TimeApiService : ITimeApiService
{
    private readonly HttpClient _http;

    private const string BaseUrl = "https://timeapi.io/api/timezone/coordinate";

    private sealed class TimeApiResponse
    {
        public string? TimeZone { get; set; }
    }

    public TimeApiService(HttpClient http)
    {
        _http = http;
        // Keep external calls snappy; on timeout we return null and degrade gracefully
        _http.Timeout = TimeSpan.FromSeconds(3);
    }

    public async Task<string?> GetTimeZoneIdAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}?latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        try
        {
            var resp = await _http.GetFromJsonAsync<TimeApiResponse>(url, cancellationToken);
            return resp?.TimeZone;
        }
        catch (OperationCanceledException)
        {
            // Includes HttpClient timeouts; treat as unavailable and return null
            return null;
        }
        catch
        {
            return null;
        }
    }
}
