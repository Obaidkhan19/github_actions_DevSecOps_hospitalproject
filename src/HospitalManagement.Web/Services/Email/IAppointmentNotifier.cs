using HospitalManagement.Web.Models;

namespace HospitalManagement.Web.Services.Email;

/// <summary>Tells the patient about an appointment change. Must never throw.</summary>
public interface IAppointmentNotifier
{
    Task NotifyAsync(Appointment appointment, AppointmentEvent appointmentEvent);
}
