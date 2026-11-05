using HospitalManagement.Web.Models;

namespace HospitalManagement.Web.Services;

public interface IAppointmentService
{
    Task<List<Appointment>> SearchAsync(string? search, AppointmentStatus? status, DateOnly? date);
    Task<Appointment?> GetAsync(int id);
    Task<AppointmentResult> CreateAsync(int patientId, int doctorId, DateOnly date, TimeOnly time, string reason);
    Task<AppointmentResult> UpdateDetailsAsync(int id, int doctorId, string reason);
    Task<AppointmentResult> RescheduleAsync(int id, DateOnly date, TimeOnly time);
    Task<AppointmentResult> CancelAsync(int id);
    Task<AppointmentResult> CompleteAsync(int id);
}
