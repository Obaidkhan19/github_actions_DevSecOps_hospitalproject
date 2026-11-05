using HospitalManagement.Web.Models;

namespace HospitalManagement.Web.Services;

public class AppointmentResult
{
    public bool Succeeded { get; private init; }
    public string? Error { get; private init; }
    public Appointment? Appointment { get; private init; }

    public static AppointmentResult Success(Appointment appointment) =>
        new() { Succeeded = true, Appointment = appointment };

    public static AppointmentResult Fail(string error) =>
        new() { Succeeded = false, Error = error };
}
