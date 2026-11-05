namespace HospitalManagement.Web.Services.Email;

/// <summary>Used when email is switched off. It sends nothing and only writes a log line.</summary>
public class NoOpEmailService : IEmailService
{
    private readonly ILogger<NoOpEmailService> _logger;

    public NoOpEmailService(ILogger<NoOpEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody)
    {
        _logger.LogInformation("Email delivery is disabled, so no email was sent.");
        return Task.CompletedTask;
    }
}
