using HospitalManagement.Web.Configuration;
using HospitalManagement.Web.Models;
using HospitalManagement.Web.Services;
using HospitalManagement.Web.Services.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HospitalManagement.Tests.Helpers;

/// <summary>
/// Gives every service test a new database, a fake email service and a fixed clock.
/// The fixed "now" is Monday 2 November 2026, 09:00.
/// </summary>
public abstract class ServiceTestBase : IDisposable
{
    protected static readonly DateOnly FutureDate = new(2026, 11, 10);
    protected static readonly DateOnly PastDate = new(2026, 11, 1);

    protected readonly TestDatabase Db = new();
    protected readonly FakeEmailService Email = new();
    protected readonly FixedTimeProvider Time =
        new(new DateTimeOffset(2026, 11, 2, 9, 0, 0, TimeSpan.Zero));

    protected readonly PatientService Patients;
    protected readonly DoctorService Doctors;
    protected readonly AppointmentService Appointments;

    protected ServiceTestBase()
    {
        var notifier = new AppointmentNotifier(
            Email,
            Options.Create(new HospitalOptions { Name = "Test Hospital" }),
            NullLogger<AppointmentNotifier>.Instance);

        Patients = new PatientService(Db.Context, Time);
        Doctors = new DoctorService(Db.Context);
        Appointments = new AppointmentService(Db.Context, notifier, Time);
    }

    protected Task<Patient> AddPatientAsync(
        string first = "Erika", string last = "Musterfrau", string email = "erika@example.com") =>
        Patients.CreateAsync(new Patient
        {
            FirstName = first,
            LastName = last,
            DateOfBirth = new DateOnly(1990, 5, 20),
            Gender = "Female",
            Phone = "+49 000 0000000",
            Email = email,
            Address = "1 Sample Street, Sampletown"
        });

    protected Task<Doctor> AddDoctorAsync(string first = "Anna", string last = "Becker") =>
        Doctors.CreateAsync(new Doctor
        {
            FirstName = first,
            LastName = last,
            Specialization = "Cardiology",
            Department = "Heart Centre"
        });

    public void Dispose() => Db.Dispose();
}
