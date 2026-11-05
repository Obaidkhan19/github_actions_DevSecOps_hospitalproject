using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.Web.Configuration;

/// <summary>
/// Settings that differ for every hospital. Set with environment variables,
/// for example: Hospital__Name="Hospital A"
/// </summary>
public class HospitalOptions
{
    public const string SectionName = "Hospital";

    [Required]
    public string Name { get; set; } = string.Empty;
}
