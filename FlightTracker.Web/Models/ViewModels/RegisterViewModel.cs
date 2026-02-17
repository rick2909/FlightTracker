using System.ComponentModel.DataAnnotations;

namespace FlightTracker.Web.Models.ViewModels;

public class RegisterViewModel
{
    [Required]
    [Display(Name = "Full Name")]
    [StringLength(64, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Username")]
    [StringLength(32, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "I accept the terms and conditions")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms and conditions.")]
    public bool AcceptTerms { get; set; }
}
