using HospitalManagement.Web.Data;
using HospitalManagement.Web.Models;
using HospitalManagement.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Web.Services;

/// <summary>All dashboard numbers come from the database.</summary>
public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _time;

    public DashboardService(ApplicationDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<DashboardViewModel> GetAsync()
    {
        var now = _time.GetLocalNow().DateTime;
        var today = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);

        return new DashboardViewModel
        {
            TotalPatients = await _db.Patients.CountAsync(),
            TotalDoctors = await _db.Doctors.CountAsync(),

            TodaysAppointmentCount = await _db.Appointments.CountAsync(a =>
                a.AppointmentDate == today && a.Status != AppointmentStatus.Cancelled),

            UpcomingAppointmentCount = await _db.Appointments.CountAsync(a =>
                a.Status == AppointmentStatus.Scheduled &&
                (a.AppointmentDate > today ||
                 (a.AppointmentDate == today && a.AppointmentTime > currentTime))),

            CompletedAppointmentCount = await _db.Appointments.CountAsync(a =>
                a.Status == AppointmentStatus.Completed),

            TodaysAppointments = await _db.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentDate == today && a.Status != AppointmentStatus.Cancelled)
                .OrderBy(a => a.AppointmentTime)
                .ToListAsync(),

            RecentPatients = await _db.Patients
                .AsNoTracking()
                .OrderByDescending(p => p.RegistrationDate)
                .ThenByDescending(p => p.Id)
                .Take(5)
                .ToListAsync()
        };
    }
}
