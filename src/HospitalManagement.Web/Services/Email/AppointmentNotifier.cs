using HospitalManagement.Web.Configuration;
using HospitalManagement.Web.Models;
using Microsoft.Extensions.Options;

namespace HospitalManagement.Web.Services.Email;

/// <summary>
/// Builds the email for an appointment event and sends it. If sending fails, the error is
/// logged (without patient information) and the appointment stays saved.
/// </summary>
public class AppointmentNotifier : IAppointmentNotifier
{
    private readonly IEmailService _email;
    private readonly HospitalOptions _hospital;
    private readonly ILogger<AppointmentNotifier> _logger;

    public AppointmentNotifier(
        IEmailService email,
        IOptions<HospitalOptions> hospital,
        ILogger<AppointmentNotifier> logger)
    {
        _email = email;
        _hospital = hospital.Value;
        _logger = logger;
    }

    public async Task NotifyAsync(Appointment appointment, AppointmentEvent appointmentEvent)
    {
        try
        {
            var patient = appointment.Patient;
            var doctor = appointment.Doctor;

            if (patient is null || doctor is null || string.IsNullOrWhiteSpace(patient.Email))
            {
                _logger.LogWarning(
                    "Appointment email skipped because details are missing. AppointmentId={AppointmentId} Event={Event}",
                    appointment.Id, appointmentEvent);
                return;
            }

            var statusLabel = appointmentEvent switch
            {
                AppointmentEvent.Created => "Scheduled",
                AppointmentEvent.Rescheduled => "Rescheduled",
                _ => "Cancelled"
            };

            var message = EmailTemplates.Build(
                appointmentEvent,
                new AppointmentEmailModel(
                    _hospital.Name,
                    patient.FullName,
                    doctor.DisplayName,
                    appointment.AppointmentDate,
                    appointment.AppointmentTime,
                    statusLabel));

            await _email.SendAsync(patient.Email, message.Subject, message.HtmlBody);

            _logger.LogInformation(
                "Appointment email sent. AppointmentId={AppointmentId} Event={Event}",
                appointment.Id, appointmentEvent);
        }
        catch (Exception ex)
        {
            // Only the error type is logged: error texts can contain email addresses.
            _logger.LogWarning(
                "Appointment email could not be sent. AppointmentId={AppointmentId} Event={Event} ErrorType={ErrorType}",
                appointment.Id, appointmentEvent, ex.GetType().Name);
        }
    }
}
