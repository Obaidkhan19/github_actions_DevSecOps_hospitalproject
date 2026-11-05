using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagement.Web.Models;

public class Patient : ITimestamped, IValidatableObject
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly DateOfBirth { get; set; }

    [Required, StringLength(20)]
    [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Choose Male, Female or Other.")]
    public string Gender { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    [Display(Name = "Phone number")]
    public string Phone { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Display(Name = "Registration date")]
    public DateTime RegistrationDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<Appointment> Appointments { get; set; } = new();

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth > DateOnly.FromDateTime(DateTime.Today))
        {
            yield return new ValidationResult(
                "Date of birth cannot be in the future.", new[] { nameof(DateOfBirth) });
        }
        else if (DateOfBirth < new DateOnly(1900, 1, 1))
        {
            yield return new ValidationResult(
                "Enter a valid date of birth.", new[] { nameof(DateOfBirth) });
        }
    }
}
