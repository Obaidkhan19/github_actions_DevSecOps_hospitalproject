using System.Net;
using System.Net.Mail;
using HospitalManagement.Web.Configuration;
using Microsoft.Extensions.Options;

namespace HospitalManagement.Web.Services.Email;

/// <summary>Sends email through an SMTP server. Credentials come from environment variables.</summary>
public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;

    public SmtpEmailService(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl
        };

        if (!string.IsNullOrEmpty(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        await client.SendMailAsync(message);
    }
}
