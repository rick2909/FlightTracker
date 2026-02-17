using System.Text.RegularExpressions;
using FlightTracker.Application.Services.Interfaces;

namespace FlightTracker.Application.Services.Implementation;

/// <summary>
/// Service implementation for username validation business rules.
/// </summary>
public sealed class UsernameValidationService : IUsernameValidationService
{
    private static readonly Regex AllowedCharacters = new("^[A-Za-z0-9_.-]+$", RegexOptions.Compiled);

    // TODO: In a real application, prohibited terms would likely come from a database or configuration file.
    private static readonly string[] ProhibitedTerms =
    {
        "badword",
        "profanity",
        "offensive",
        "admin",
        "moderator",
        "support",
        "helpdesk",
        "contact",
        "root",
    };

    public Task<UsernameValidationResult> ValidateAsync(string username, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(username))
        {
            return Task.FromResult(new UsernameValidationResult(
                false,
                "Username is required."
            ));
        }

        if (!AllowedCharacters.IsMatch(username))
        {
            return Task.FromResult(new UsernameValidationResult(
                false,
                "Username can only contain letters, numbers, underscore, hyphen, or dot."
            ));
        }

        if (ProhibitedTerms.Any(term => username.Contains(term, StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult(new UsernameValidationResult(
                false,
                "Username contains prohibited terms."
            ));
        }

        return Task.FromResult(new UsernameValidationResult(true, null));
    }
}
