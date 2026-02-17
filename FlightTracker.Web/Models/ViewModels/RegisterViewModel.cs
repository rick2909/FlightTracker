using System.ComponentModel.DataAnnotations;

namespace FlightTracker.Web.Models.ViewModels;

public class RegisterViewModel
{
    [Display(Name = "Full Name")]
    [StringLength(64, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

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

    [Display(Name = "I accept the terms and conditions")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms and conditions.")]
    public bool AcceptTerms { get; set; }
}
