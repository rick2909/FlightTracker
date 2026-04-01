using FlightTracker.Web.Api.Contracts;

namespace FlightTracker.Web.Services.Interfaces;

public interface IFlightsApiClient
{
    Task<IReadOnlyList<FlightLookupCandidateApiResponse>> LookupByDesignatorAsync(
        string designator,
        DateOnly? date,
        CancellationToken cancellationToken = default);
}
