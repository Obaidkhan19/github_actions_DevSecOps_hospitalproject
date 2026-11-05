using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagement.Web.Models;

public class Doctor : ITimestamped
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Specialization { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Department { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<Appointment> Appointments { get; set; } = new();

    [NotMapped]
    public string DisplayName => $"Dr. {FirstName} {LastName}";
}
