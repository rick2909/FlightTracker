using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using FlightTracker.Infrastructure.Data;

namespace FlightTracker.Web.Validation;

public sealed class UsernamePolicyValidator : IUserValidator<ApplicationUser>
{
    private static readonly Regex AllowedCharacters = new("^[A-Za-z0-9_.-]+$", RegexOptions.Compiled);

    // TODO In a real application, prohibited terms would likely come from a database or configuration file.
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

    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        var errors = new List<IdentityError>();
        var userName = user.UserName ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userName))
        {
            errors.Add(new IdentityError
            {
                Code = "InvalidUserName",
                Description = "Username is required."
            });

            return Task.FromResult(IdentityResult.Failed(errors.ToArray()));
        }

        if (!AllowedCharacters.IsMatch(userName))
        {
            errors.Add(new IdentityError
            {
                Code = "InvalidUserNameCharacters",
                Description = "Username can only contain letters, numbers, underscore, hyphen, or dot."
            });
        }

        var lowered = userName.ToLowerInvariant();
        if (ProhibitedTerms.Any(term => lowered.Contains(term, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add(new IdentityError
            {
                Code = "ProhibitedUserName",
                Description = "Username contains prohibited terms."
            });
        }

        return Task.FromResult(errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray()));
    }
}
