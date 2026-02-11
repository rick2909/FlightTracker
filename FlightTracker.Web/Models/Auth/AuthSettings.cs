namespace FlightTracker.Web.Models.Auth;

public class AuthSettings
{
    public const string SectionName = "Auth";

    public bool EnableDevBypass { get; set; }

    public int DevUserId { get; set; }

    public string DevUserName { get; set; } = string.Empty;

    public string DevEmail { get; set; } = string.Empty;

    public string DevDisplayName { get; set; } = "Developer";
}
