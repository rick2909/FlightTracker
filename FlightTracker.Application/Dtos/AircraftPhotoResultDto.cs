namespace FlightTracker.Application.Dtos;

/// <summary>
/// Result of aircraft photo lookup from airport-data.com API.
/// </summary>
public record AircraftPhotoResultDto
{
    /// <summary>
    /// HTTP status code (200 = success).
    /// </summary>
    public int Status { get; init; }

    /// <summary>
    /// Number of results found.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// List of aircraft photos found.
    /// </summary>
    public IReadOnlyList<AircraftPhotoDto> Data { get; init; } = new List<AircraftPhotoDto>();

    /// <summary>
    /// Error message if unsuccessful.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Returns whether the lookup was successful.
    /// </summary>
    public bool IsSuccess => Status == 200 && Count > 0;
}
