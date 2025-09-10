using System.ComponentModel.DataAnnotations;

namespace Symphora.Models.ViewModels;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, ErrorMessage = "The {0} must be at max {1} characters long.")]
    [Display(Name = "Full Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Timezone is required.")]
    [Display(Name = "Timezone")]
    public string Timezone { get; set; } = "UTC";

    [Display(Name = "Avatar")]
    [DataType(DataType.Upload)]
    public IFormFile? AvatarFile { get; set; }

    [Display(Name = "Current Avatar")]
    public string? CurrentAvatarUrl { get; set; }

    // For displaying success/error messages
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}