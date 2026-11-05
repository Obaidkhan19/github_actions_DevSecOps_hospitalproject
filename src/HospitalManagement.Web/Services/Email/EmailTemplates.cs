using System.Globalization;
using System.Net;

namespace HospitalManagement.Web.Services.Email;

public record AppointmentEmailModel(
    string HospitalName,
    string PatientName,
    string DoctorName,
    DateOnly Date,
    TimeOnly Time,
    string StatusLabel);

public record EmailMessage(string Subject, string HtmlBody);

/// <summary>HTML email templates. All values are HTML-encoded.</summary>
public static class EmailTemplates
{
    public static EmailMessage Build(AppointmentEvent appointmentEvent, AppointmentEmailModel model)
    {
        var (subject, headline, intro) = appointmentEvent switch
        {
            AppointmentEvent.Created => (
                $"Appointment confirmed - {model.HospitalName}",
                "Your appointment is confirmed",
                "A new appointment has been scheduled for you."),
            AppointmentEvent.Rescheduled => (
                $"Appointment rescheduled - {model.HospitalName}",
                "Your appointment has been rescheduled",
                "Your appointment details have changed. Please note the new details below."),
            AppointmentEvent.Cancelled => (
                $"Appointment cancelled - {model.HospitalName}",
                "Your appointment has been cancelled",
                "The following appointment has been cancelled. Please contact us to book a new one."),
            _ => throw new ArgumentOutOfRangeException(nameof(appointmentEvent))
        };

        var date = model.Date.ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture);
        var time = model.Time.ToString("HH:mm", CultureInfo.InvariantCulture);

        var html = $$"""
        <!DOCTYPE html>
        <html lang="en">
        <body style="margin:0;padding:0;background-color:#f4f6f8;font-family:Arial,Helvetica,sans-serif;color:#212529;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f6f8;padding:24px 0;">
            <tr>
              <td align="center">
                <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="background-color:#ffffff;border-radius:6px;">
                  <tr>
                    <td style="background-color:#0d6efd;color:#ffffff;padding:20px 28px;font-size:20px;font-weight:bold;">{{E(model.HospitalName)}}</td>
                  </tr>
                  <tr>
                    <td style="padding:28px;">
                      <h1 style="margin:0 0 12px 0;font-size:20px;">{{E(headline)}}</h1>
                      <p style="margin:0 0 20px 0;line-height:1.5;">Dear {{E(model.PatientName)}},<br />{{E(intro)}}</p>
                      <table role="presentation" width="100%" cellpadding="8" cellspacing="0" style="border:1px solid #dee2e6;border-collapse:collapse;">
                        <tr><td style="border:1px solid #dee2e6;background-color:#f8f9fa;width:35%;"><strong>Doctor</strong></td><td style="border:1px solid #dee2e6;">{{E(model.DoctorName)}}</td></tr>
                        <tr><td style="border:1px solid #dee2e6;background-color:#f8f9fa;"><strong>Date</strong></td><td style="border:1px solid #dee2e6;">{{E(date)}}</td></tr>
                        <tr><td style="border:1px solid #dee2e6;background-color:#f8f9fa;"><strong>Time</strong></td><td style="border:1px solid #dee2e6;">{{E(time)}}</td></tr>
                        <tr><td style="border:1px solid #dee2e6;background-color:#f8f9fa;"><strong>Status</strong></td><td style="border:1px solid #dee2e6;">{{E(model.StatusLabel)}}</td></tr>
                      </table>
                      <p style="margin:20px 0 0 0;font-size:13px;color:#6c757d;">This is an automatic message from {{E(model.HospitalName)}}. Please do not reply to this email.</p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

        return new EmailMessage(subject, html);
    }

    private static string E(string value) => WebUtility.HtmlEncode(value);
}
