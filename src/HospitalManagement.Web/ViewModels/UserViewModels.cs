using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.Web.ViewModels;

public class UserListItem
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UserFormViewModel
{
    public string? Id { get; set; }

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string? ConfirmPassword { get; set; }

    public bool IsEdit => Id is not null;
}
