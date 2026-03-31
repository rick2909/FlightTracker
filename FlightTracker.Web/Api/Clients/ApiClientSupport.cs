using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Web.Api.Clients;

internal static class ApiClientSupport
{
    public static async Task<T?> ReadRequiredAsync<T>(
        this HttpClient httpClient,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            requestUri,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    public static async Task<T?> SendRequiredAsync<T>(
        this HttpClient httpClient,
        HttpMethod method,
        string requestUri,
        object? content = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, requestUri);
        if (content is not null)
        {
            request.Content = JsonContent.Create(content);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        var message = problem?.Detail
            ?? problem?.Title
            ?? $"Request failed with status {(int)response.StatusCode}.";

        throw new InvalidOperationException(message);
    }
}
