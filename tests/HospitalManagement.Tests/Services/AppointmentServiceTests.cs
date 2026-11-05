using HospitalManagement.Tests.Helpers;
using HospitalManagement.Web.Models;

namespace HospitalManagement.Tests.Services;

public class AppointmentServiceTests : ServiceTestBase
{
    private static readonly TimeOnly Ten = new(10, 0);

    [Fact]
    public async Task Create_saves_a_scheduled_appointment()
    {
        var patient = await AddPatientAsync();
        var doctor = await AddDoctorAsync();

        var result = await Appointments.CreateAsync(patient.Id, doctor.Id, FutureDate, Ten, "  Check-up  ");

        Assert.True(result.Succeeded);
        var saved = await Appointments.GetAsync(result.Appointment!.Id);
        Assert.NotNull(saved);
        Assert.Equal(AppointmentStatus.Scheduled, saved!.Status);
        Assert.Equal("Check-up", saved.Reason);
        Assert.Equal(patient.Id, saved.PatientId);
        Assert.Equal(doctor.Id, saved.DoctorId);
    }

    [Fact]
    public async Task Create_fails_for_an_unknown_patient_or_doctor()
    {
        var patient = await AddPatientAsync();
        var doctor = await AddDoctorAsync();

        var badPatient = await Appointments.CreateAsync(999, doctor.Id, FutureDate, Ten, "x");
        var badDoctor = await Appointments.CreateAsync(patient.Id, 999, FutureDate, Ten, "x");

        Assert.False(badPatient.Succeeded);
        Assert.False(badDoctor.Succeeded);
    }

    [Fact]
    public async Task Create_rejects_an_appointment_in_the_past()
    {
        var patient = await AddPatientAsync();
        var doctor = await AddDoctorAsync();

        var result = await Appointments.CreateAsync(patient.Id, doctor.Id, PastDate, Ten, "Check-up");

        Assert.False(result.Succeeded);
        Assert.Contains("future", result.Error);
    }

    [Theory]
    [InlineData(7, 30)]
    [InlineData(18, 0)]
    [InlineData(22, 0)]
    public async Task Create_rejects_times_outside_opening_hours(int hour, int minute)
    {
        var patient = await AddPatientAsync();
        var doctor = await AddDoctorAsync();

        var result = await Appointments.CreateAsync(
            patient.Id, doctor.Id, FutureDate, new TimeOnly(hour, minute), "Check-up");

        Assert.False(result.Succeeded);
        Assert.Contains("08:00", result.Error);
    }

    [Fact]
    public async Task Doctor_cannot_be_booked_twice_at_the_same_time()
    {
        var first = await AddPatientAsync("Erika", "Musterfrau", "erika@example.com");
        var second = await AddPatientAsync("Max", "Mustermann", "max@example.com");
        var doctor = await AddDoctorAsync();

        var one = await Appointments.CreateAsync(first.Id, doctor.Id, FutureDate, Ten, "Check-up");
        var two = await Appointments.CreateAsync(second.Id, doctor.Id, FutureDate, Ten, "Check-up");

        Assert.True(one.Succeeded);
        Assert.False(two.Succeeded);
        Assert.Contains("already has an appointment", two.Error);
    }

    [Fact]
    public async Task Overlapping_appointments_for_the_same_doctor_are_rejected()
    {
        var first = await AddPatientAsync("Erika", "Musterfrau", "erika@example.com");
        var second = await AddPatientAsync("Max", "Mustermann", "max@example.com");
        var doctor = await AddDoctorAsync();
        await Appointments.CreateAsync(first.Id, doctor.Id, FutureDate, Ten, "Check-up");

        var tooClose = await Appointments.CreateAsync(second.Id, doctor.Id, FutureDate, new TimeOnly(10, 15), "Check-up");
        var afterSlot = await Appointments.CreateAsync(second.Id, doctor.Id, FutureDate, new TimeOnly(10, 30), "Check-up");

        Assert.False(tooClose.Succeeded);
        Assert.True(afterSlot.Succeeded);
    }

    [Fact]
    public async Task Different_doctors_can_be_booked_at_the_same_time()
    {
        var first = await AddPatientAsync("Erika", "Musterfrau", "erika@example.com");
        var second = await AddPatientAsync("Max", "Mustermann", "max@example.com");
        var doctorA = await AddDoctorAsync("Anna", "Becker");
        var doctorB = await AddDoctorAsync("Tom", "Keller");

        var one = await Appointments.CreateAsync(first.Id, doctorA.Id, FutureDate, Ten, "Check-up");
        var two = await Appointments.CreateAsync(second.Id, doctorB.Id, FutureDate, Ten, "Check-up");

        Assert.True(one.Succeeded);
        Assert.True(two.Succeeded);
    }

    [Fact]
    public async Task A_cancelled_slot_can_be_booked_again()
    {
        var first = await AddPatientAsync("Erika", "Musterfrau", "erika@example.com");
        var second = await AddPatientAsync("Max", "Mustermann", "max@example.com");
        var doctor = await AddDoctorAsync();
        var booked = await Appointments.CreateAsync(first.Id, doctor.Id, FutureDate, Ten, "Check-up");
        await Appointments.CancelAsync(booked.Appointment!.Id);

        var again = await Appointments.CreateAsync(second.Id, doctor.Id, FutureDate, Ten, "Check-up");

        Assert.True(again.Succeeded);
    }

    [Fact]
    public async Task Reschedule_moves_the_appointment()
    {
        var patient = await AddPatientAsync();
        var doctor = await AddDoctorAsync();
        var booked = await Appointments.CreateAsync(patient.Id, doctor.Id, FutureDate, Ten, "Check-up");

        var result = await Appointments.RescheduleAsync(booked.Appointment!.Id, FutureDate.AddDays(1), new TimeOnly(14, 0));

        Assert.True(result.Succeeded);
        var saved = await Appointments.GetAsync(booked.Appointment.Id);
        Assert.Equal(FutureDate.AddDays(1), saved!.AppointmentDate);
        Assert.Equal(new TimeOnly(14, 0), saved.AppointmentTime);
    }

    [Fact]
    public async Task Reschedule_to_a_taken_slot_is_rejected()
    {
        var first = await AddPatientAsync("Erika", "Musterfrau", "erika@example.com");
        var second = await AddPatientAsync("Max", "Mustermann", "max@example.com");
        var doctor = await AddDoctorAsync();
        await Appointments.CreateAsync(first.Id, doctor.Id, FutureDate, Ten, "Check-up");
        var other = await Appointments.CreateAsync(second.Id, doctor.Id, FutureDate, new TimeOnly(12, 0), "Check-up");

        var result = await Appointments.RescheduleAsync(other.Appointment!.Id, FutureDate, Ten);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Reschedule_to_a_nearby_time_of_the_same_appointment_is_allowed()
    {
        var patient = await AddPatientAsync();
        var doctor = await AddDoctorAsync();
        var booked = await Appointments.CreateAsync(patient.Id, doctor.Id, FutureDate, Ten, "Check-up");

        var result = await Appointments.RescheduleAsync(booked.Appointment!.Id, FutureDate, new TimeOnly(10, 15));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task UpdateDetails_changes_the_reason()
    {
        var patient = await AddPatientAsync();
        var doctor = await AddDoctorAsync();
        var booked = await Appointments.CreateAsync(patient.Id, doctor.Id, FutureDate, Ten, "Check-up");

        var result = await Appointments.UpdateDetailsAsync(booked.Appointment!.Id, doctor.Id, "Follow-up visit");

        Assert.True(result.Succeeded);
        var saved = await Appointments.GetAsync(booked.Appointment.Id);
        Assert.Equal("Follow-up visit", saved!.Reason);
    }

    [Fact]
    public async Task UpdateDetails_to_a_busy_doctor_is_rejected()
    {
        var first = await AddPatientAsync("Erika", "Musterfrau", "erika@example.com");
        var second = await AddPatientAsync("Max", "Mustermann", "max@example.com");
        var doctorA = await AddDoctorAsync("Anna", "Becker");
        var doctorB = await AddDoctorAsync("Tom", "Keller");
        await Appointments.CreateAsync(first.Id, doctorB.Id, FutureDate, Ten, "Check-up");
        var booked = await Appointments.CreateAsync(second.Id, doctorA.Id, FutureDate, Ten, "Check-up");

        var result = await Appointments.UpdateDetailsAsync(booked.Appointment!.Id, doctorB.Id, "Check-up");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Cancel_and_complete_change_the_status()
    {
        var patient = await AddPatientAsync();
        var doctor = await AddDoctorAsync();
        var toCancel = await Appointments.CreateAsync(patient.Id, doctor.Id, FutureDate, Ten, "Check-up");
        var toComplete = await Appointments.CreateAsync(patient.Id, doctor.Id, FutureDate, new TimeOnly(12, 0), "Check-up");

        await Appointments.CancelAsync(toCancel.Appointment!.Id);
        await Appointments.CompleteAsync(toComplete.Appointment!.Id);

        Assert.Equal(AppointmentStatus.Cancelled, (await Appointments.GetAsync(toCancel.Appointment.Id))!.Status);
        Assert.Equal(AppointmentStatus.Completed, (await Appointments.GetAsync(toComplete.Appointment.Id))!.Status);
    }

    [Fact]
    public async Task Only_scheduled_appointments_can_be_cancelled_again()
    {
        var patient = await AddPatientAsync();
        var doctor = await AddDoctorAsync();
        var booked = await Appointments.CreateAsync(patient.Id, doctor.Id, FutureDate, Ten, "Check-up");
        await Appointments.CancelAsync(booked.Appointment!.Id);

        var again = await Appointments.CancelAsync(booked.Appointment.Id);

        Assert.False(again.Succeeded);
    }

    [Fact]
    public async Task Search_filters_by_status_date_and_text()
    {
        var patient = await AddPatientAsync("Erika", "Musterfrau", "erika@example.com");
        var doctor = await AddDoctorAsync();
        var one = await Appointments.CreateAsync(patient.Id, doctor.Id, FutureDate, Ten, "Knee pain");
        await Appointments.CreateAsync(patient.Id, doctor.Id, FutureDate.AddDays(1), Ten, "Check-up");
        await Appointments.CancelAsync(one.Appointment!.Id);

        var cancelled = await Appointments.SearchAsync(null, AppointmentStatus.Cancelled, null);
        var onDate = await Appointments.SearchAsync(null, null, FutureDate.AddDays(1));
        var byText = await Appointments.SearchAsync("knee", null, null);
        var byPatient = await Appointments.SearchAsync("muster", null, null);

        Assert.Single(cancelled);
        Assert.Single(onDate);
        Assert.Single(byText);
        Assert.Equal(2, byPatient.Count);
    }
}
