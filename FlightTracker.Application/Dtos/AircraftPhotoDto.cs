namespace FlightTracker.Application.Dtos;

/// <summary>
/// Represents a single aircraft photo result from airport-data.com API.
/// </summary>
public record AircraftPhotoDto
{
    /// <summary>
    /// Thumbnail image URL (200px width).
    /// </summary>
    public required string Image { get; init; }

    /// <summary>
    /// Link to the full-resolution photo on airport-data.com.
    /// </summary>
    public required string Link { get; init; }

    /// <summary>
    /// Name of the photographer.
    /// </summary>
    public required string Photographer { get; init; }
}
