namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Service for validating username according to business rules.
/// </summary>
public interface IUsernameValidationService
{
    /// <summary>
    /// Validates a username for allowed characters and prohibited terms.
    /// </summary>
    /// <param name="username">The username to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with error messages if invalid.</returns>
    Task<UsernameValidationResult> ValidateAsync(string username, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of username validation.
/// </summary>
public record UsernameValidationResult(bool IsValid, string? ErrorMessage);
