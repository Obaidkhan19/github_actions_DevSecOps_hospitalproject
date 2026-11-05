namespace HospitalManagement.Web.Configuration;

/// <summary>
/// Optional first-start data. The admin user is only created when the database has no users yet.
/// </summary>
public class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string AdminName { get; set; } = "Administrator";

    /// <summary>Adds a few synthetic doctors and patients when the tables are empty.</summary>
    public bool SampleData { get; set; }
}
