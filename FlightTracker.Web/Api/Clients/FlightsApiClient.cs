using FlightTracker.Web.Api.Contracts;
using FlightTracker.Web.Services.Interfaces;

namespace FlightTracker.Web.Api.Clients;

public sealed class FlightsApiClient(HttpClient httpClient) : IFlightsApiClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<IReadOnlyList<FlightLookupCandidateApiResponse>> LookupByDesignatorAsync(
        string designator,
        DateOnly? date,
        CancellationToken cancellationToken = default)
    {
        var query = $"designator={Uri.EscapeDataString(designator)}";
        if (date.HasValue)
        {
            query += $"&date={date.Value:yyyy-MM-dd}";
        }

        var response = await _httpClient.ReadRequiredAsync<List<FlightLookupCandidateApiResponse>>(
            $"/api/v1/flights/lookup?{query}",
            cancellationToken);

        return response ?? new List<FlightLookupCandidateApiResponse>();
    }
}
