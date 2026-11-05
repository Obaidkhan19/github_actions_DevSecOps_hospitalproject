using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HospitalManagement.Web.Models;

/// <summary>A staff member who can sign in. Stored in the Users table.</summary>
public class ApplicationUser : IdentityUser, ITimestamped
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
