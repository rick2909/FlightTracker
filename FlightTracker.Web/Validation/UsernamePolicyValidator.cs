using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using FlightTracker.Infrastructure.Data;

namespace FlightTracker.Web.Validation;

public sealed class UsernamePolicyValidator : IUserValidator<ApplicationUser>
{
    private static readonly Regex AllowedCharacters = new("^[A-Za-z0-9_.-]+$", RegexOptions.Compiled);

    private static readonly string[] ProhibitedTerms =
    {
        "badword",
        "profanity",
        "offensive"
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
        }
        else
        {
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
        }

        return Task.FromResult(errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray()));
    }
}
