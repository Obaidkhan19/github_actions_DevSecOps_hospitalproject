using HospitalManagement.Web.Data;
using HospitalManagement.Web.Models;
using HospitalManagement.Web.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Web.Services;

/// <summary>
/// Appointment rules: valid date and time, no double booking for a doctor.
/// Emails are sent by a separate notifier, so a mail problem never breaks a booking.
/// </summary>
public class AppointmentService : IAppointmentService
{
    /// <summary>Every appointment blocks the doctor for this many minutes.</summary>
    public const int SlotMinutes = 30;

    public static readonly TimeOnly FirstSlot = new(8, 0);
    public static readonly TimeOnly LastSlot = new(17, 30);

    private const string ConflictMessage = "This doctor already has an appointment at that time.";

    private readonly ApplicationDbContext _db;
    private readonly IAppointmentNotifier _notifier;
    private readonly TimeProvider _time;

    public AppointmentService(ApplicationDbContext db, IAppointmentNotifier notifier, TimeProvider time)
    {
        _db = db;
        _notifier = notifier;
        _time = time;
    }

    public async Task<List<Appointment>> SearchAsync(string? search, AppointmentStatus? status, DateOnly? date)
    {
        IQueryable<Appointment> query = _db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor);

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (date.HasValue)
        {
            query = query.Where(a => a.AppointmentDate == date.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a =>
                a.Patient!.FirstName.Contains(term) ||
                a.Patient!.LastName.Contains(term) ||
                a.Doctor!.FirstName.Contains(term) ||
                a.Doctor!.LastName.Contains(term) ||
                a.Reason.Contains(term));
        }

        return await query
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.AppointmentTime)
            .ToListAsync();
    }

    public Task<Appointment?> GetAsync(int id) =>
        _db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<AppointmentResult> CreateAsync(
        int patientId, int doctorId, DateOnly date, TimeOnly time, string reason)
    {
        var patient = await _db.Patients.FindAsync(patientId);
        if (patient is null)
        {
            return AppointmentResult.Fail("The selected patient does not exist.");
        }

        var doctor = await _db.Doctors.FindAsync(doctorId);
        if (doctor is null)
        {
            return AppointmentResult.Fail("The selected doctor does not exist.");
        }

        var error = await ValidateSlotAsync(doctorId, date, time, excludeAppointmentId: null);
        if (error is not null)
        {
            return AppointmentResult.Fail(error);
        }

        var appointment = new Appointment
        {
            Patient = patient,
            Doctor = doctor,
            AppointmentDate = date,
            AppointmentTime = time,
            Reason = reason.Trim(),
            Status = AppointmentStatus.Scheduled
        };

        _db.Appointments.Add(appointment);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Another booking took the slot at the same moment (database unique index).
            return AppointmentResult.Fail(ConflictMessage);
        }

        await _notifier.NotifyAsync(appointment, AppointmentEvent.Created);
        return AppointmentResult.Success(appointment);
    }

    public async Task<AppointmentResult> UpdateDetailsAsync(int id, int doctorId, string reason)
    {
        var appointment = await LoadTrackedAsync(id);
        if (appointment is null)
        {
            return AppointmentResult.Fail("Appointment not found.");
        }

        if (appointment.Status != AppointmentStatus.Scheduled)
        {
            return AppointmentResult.Fail("Only scheduled appointments can be edited.");
        }

        var doctorChanged = appointment.DoctorId != doctorId;
        if (doctorChanged)
        {
            var doctor = await _db.Doctors.FindAsync(doctorId);
            if (doctor is null)
            {
                return AppointmentResult.Fail("The selected doctor does not exist.");
            }

            var error = await ValidateSlotAsync(
                doctorId, appointment.AppointmentDate, appointment.AppointmentTime, appointment.Id);
            if (error is not null)
            {
                return AppointmentResult.Fail(error);
            }

            appointment.Doctor = doctor;
            appointment.DoctorId = doctor.Id;
        }

        appointment.Reason = reason.Trim();

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return AppointmentResult.Fail(ConflictMessage);
        }

        if (doctorChanged)
        {
            await _notifier.NotifyAsync(appointment, AppointmentEvent.Rescheduled);
        }

        return AppointmentResult.Success(appointment);
    }

    public async Task<AppointmentResult> RescheduleAsync(int id, DateOnly date, TimeOnly time)
    {
        var appointment = await LoadTrackedAsync(id);
        if (appointment is null)
        {
            return AppointmentResult.Fail("Appointment not found.");
        }

        if (appointment.Status != AppointmentStatus.Scheduled)
        {
            return AppointmentResult.Fail("Only scheduled appointments can be rescheduled.");
        }

        var error = await ValidateSlotAsync(appointment.DoctorId, date, time, appointment.Id);
        if (error is not null)
        {
            return AppointmentResult.Fail(error);
        }

        appointment.AppointmentDate = date;
        appointment.AppointmentTime = time;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return AppointmentResult.Fail(ConflictMessage);
        }

        await _notifier.NotifyAsync(appointment, AppointmentEvent.Rescheduled);
        return AppointmentResult.Success(appointment);
    }

    public async Task<AppointmentResult> CancelAsync(int id)
    {
        var appointment = await LoadTrackedAsync(id);
        if (appointment is null)
        {
            return AppointmentResult.Fail("Appointment not found.");
        }

        if (appointment.Status != AppointmentStatus.Scheduled)
        {
            return AppointmentResult.Fail("Only scheduled appointments can be cancelled.");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        await _db.SaveChangesAsync();

        await _notifier.NotifyAsync(appointment, AppointmentEvent.Cancelled);
        return AppointmentResult.Success(appointment);
    }

    public async Task<AppointmentResult> CompleteAsync(int id)
    {
        var appointment = await LoadTrackedAsync(id);
        if (appointment is null)
        {
            return AppointmentResult.Fail("Appointment not found.");
        }

        if (appointment.Status != AppointmentStatus.Scheduled)
        {
            return AppointmentResult.Fail("Only scheduled appointments can be completed.");
        }

        appointment.Status = AppointmentStatus.Completed;
        await _db.SaveChangesAsync();

        return AppointmentResult.Success(appointment);
    }

    private Task<Appointment?> LoadTrackedAsync(int id) =>
        _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

    /// <summary>Returns an error message, or null when the slot is valid and free.</summary>
    private async Task<string?> ValidateSlotAsync(
        int doctorId, DateOnly date, TimeOnly time, int? excludeAppointmentId)
    {
        var now = _time.GetLocalNow().DateTime;
        if (date.ToDateTime(time) < now)
        {
            return "The appointment must be in the future.";
        }

        if (time < FirstSlot || time > LastSlot)
        {
            return "Appointments are available between 08:00 and 17:30.";
        }

        var excludeId = excludeAppointmentId ?? 0;
        var bookedTimes = await _db.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorId == doctorId
                        && a.AppointmentDate == date
                        && a.Status != AppointmentStatus.Cancelled
                        && a.Id != excludeId)
            .Select(a => a.AppointmentTime)
            .ToListAsync();

        foreach (var booked in bookedTimes)
        {
            var difference = Math.Abs((time.ToTimeSpan() - booked.ToTimeSpan()).TotalMinutes);
            if (difference < SlotMinutes)
            {
                return ConflictMessage;
            }
        }

        return null;
    }
}
