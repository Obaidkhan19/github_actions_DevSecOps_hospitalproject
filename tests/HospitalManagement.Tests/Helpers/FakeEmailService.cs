using HospitalManagement.Web.Services.Email;

namespace HospitalManagement.Tests.Helpers;

public record SentEmail(string To, string Subject, string HtmlBody);

/// <summary>Collects emails instead of sending them. Can also simulate an SMTP failure.</summary>
public class FakeEmailService : IEmailService
{
    public List<SentEmail> Sent { get; } = new();
    public bool ShouldFail { get; set; }

    public Task SendAsync(string to, string subject, string htmlBody)
    {
        if (ShouldFail)
        {
            throw new InvalidOperationException("The SMTP server is not reachable.");
        }

        Sent.Add(new SentEmail(to, subject, htmlBody));
        return Task.CompletedTask;
    }
}
