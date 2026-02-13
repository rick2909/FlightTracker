using System.ComponentModel.DataAnnotations;

namespace FlightTracker.Web.Models.ViewModels;

public class RegisterViewModel
{
    [Display(Name = "Display name")]
    [StringLength(64, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [Display(Name = "Username")]
    [StringLength(32, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool AcceptTerms { get; set; }
}
