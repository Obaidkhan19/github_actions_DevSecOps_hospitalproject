namespace HospitalManagement.Web.Services.Email;

/// <summary>Sends one HTML email. Knows nothing about appointments.</summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);
}
