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

namespace FlightTracker.Infrastructure.External;

/// <summary>
/// Implementation of IAircraftPhotoService using airport-data.com API.
/// </summary>
public class AircraftPhotoService : IAircraftPhotoService
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://airport-data.com/api/ac_thumb.json";

    private static readonly JsonSerializerOptions s_Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public AircraftPhotoService(HttpClient http)
    {
        _http = http;
    }

    public async Task<AircraftPhotoResultDto?> GetAircraftPhotosAsync(
        string? modeSCode,
        string? registration,
        int maxResults = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modeSCode) && string.IsNullOrWhiteSpace(registration))
        {
            return null;
        }

        var url = BuildUrl(modeSCode, registration, maxResults);
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                return new AircraftPhotoResultDto
                {
                    Status = (int)resp.StatusCode,
                    Count = 0,
                    Error = $"HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}"
                };
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<AircraftPhotoResultDto>(
                stream,
                s_Json,
                cancellationToken);

            return result ?? new AircraftPhotoResultDto { Status = 500, Error = "Failed to deserialize response" };
        }
        catch (HttpRequestException ex)
        {
            return new AircraftPhotoResultDto
            {
                Status = 0,
                Count = 0,
                Error = $"Request failed: {ex.Message}"
            };
        }
        catch (JsonException ex)
        {
            return new AircraftPhotoResultDto
            {
                Status = 500,
                Count = 0,
                Error = $"JSON deserialization failed: {ex.Message}"
            };
        }
        catch (OperationCanceledException)
        {
            return new AircraftPhotoResultDto
            {
                Status = 0,
                Count = 0,
                Error = "Request canceled"
            };
        }
        catch (Exception ex)
        {
            return new AircraftPhotoResultDto
            {
                Status = 500,
                Count = 0,
                Error = $"Unexpected error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Builds the API URL with Mode-S code and/or registration.
    /// The API searches in order: (1) Both, (2) Mode-S alone, (3) Registry alone.
    /// </summary>
    private static string? BuildUrl(string? modeSCode, string? registration, int maxResults)
    {
        // Validate Mode-S code (6 hex characters)
        if (!string.IsNullOrWhiteSpace(modeSCode))
        {
            modeSCode = modeSCode.Trim().ToUpperInvariant();
            if (modeSCode.Length != 6 || !IsHexString(modeSCode))
            {
                modeSCode = null;
            }
        }

        // URL-encode registration
        if (!string.IsNullOrWhiteSpace(registration))
        {
            registration = Uri.EscapeDataString(registration.Trim());
        }

        if (maxResults <= 0)
        {
            maxResults = 1;
        }
        else if (maxResults > 5)
        {
            maxResults = 5;
        }

        // Build URL according to API preference order
        if (!string.IsNullOrWhiteSpace(modeSCode) && !string.IsNullOrWhiteSpace(registration))
        {
            return $"{BaseUrl}?m={modeSCode}&r={registration}&n={maxResults}";
        }

        if (!string.IsNullOrWhiteSpace(modeSCode))
        {
            return $"{BaseUrl}?m={modeSCode}&n={maxResults}";
        }

        if (!string.IsNullOrWhiteSpace(registration))
        {
            return $"{BaseUrl}?r={registration}&n={maxResults}";
        }

        return null;
    }

    private static bool IsHexString(string value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Length == 6 &&
               int.TryParse(value, NumberStyles.HexNumber, null, out _);
    }
}
