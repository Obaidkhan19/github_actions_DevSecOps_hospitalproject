namespace HospitalManagement.Web.Configuration;

/// <summary>SMTP settings. Set with environment variables, for example Email__Host.</summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "no-reply@hospital.example";
    public string FromName { get; set; } = "Hospital";
}
