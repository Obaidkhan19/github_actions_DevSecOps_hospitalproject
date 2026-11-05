using HospitalManagement.Tests.Helpers;
using HospitalManagement.Web.Models;

namespace HospitalManagement.Tests.Services;

public class EmailNotificationTests : ServiceTestBase
{
    private static readonly TimeOnly Ten = new(10, 0);

    [Fact]
    public async Task Creating_an_appointment_emails_the_patient()
    {
        var patient = await AddPatientAsync("Erika", "Musterfrau", "erika@example.com");
        var doctor = await AddDoctorAsync("Anna", "Becker");

        await Appointments.CreateAsync(patient.Id, doctor.Id, FutureDate, Ten, "Check-up");

        var email = Assert.Single(Email.Sent);
        Assert.Equal("erika@example.com", email.To);
        Assert.Contains("confirmed", email.Subject);
        Assert.Contains("Test Hospital", email.HtmlBody);
        Assert.Contains("Erika Musterfrau", email.HtmlBody);
        Assert.Contains("Dr. Anna Becker", email.HtmlBody);
        Assert.Contains("10:00", email.HtmlBody);
        Assert.Contains("Scheduled", email.HtmlBody);
    }

    [Fact]
    public async Task Rescheduling_emails_the_patient()
    {
        var patient = await AddPatientAsync();
        var doctor = await AddDoctorAsync();
        var booked = await Appointments.CreateAsync(patient.Id, doctor.Id, FutureDate, Ten, "Check-up");
        Email.Sent.Clear();

        await Appointments.RescheduleAsync(booked.Appointment!.Id, FutureDate.AddDays(1), new TimeOnly(14, 30));

        var email = Assert.Single(Email.Sent);
        Assert.Contains("rescheduled", email.Subject);
        Assert.Contains("14:30", email.HtmlBody);
        Assert.Contains("Rescheduled", email.HtmlBody);
    }

    [Fact]
    public async Task Cancelling_emails_the_patient()
    {
        var patient = await AddPatientAsync();
        var doctor = await AddDoctorAsync();
        var booked = await Appointments.CreateAsync(patient.Id, doctor.Id, FutureDate, Ten, "Check-up");
        Email.Sent.Clear();

        await Appointments.CancelAsync(booked.Appointment!.Id);

        var email = Assert.Single(Email.Sent);
        Assert.Contains("cancelled", email.Subject);
        Assert.Contains("Cancelled", email.HtmlBody);
    }

    [Fact]
    public async Task Completing_or_failing_to_book_sends_no_email()
    {
        var patient = await AddPatientAsync();
        var doctor = await AddDoctorAsync();
        var booked = await Appointments.CreateAsync(patient.Id, doctor.Id, FutureDate, Ten, "Check-up");
        Email.Sent.Clear();

        await Appointments.CompleteAsync(booked.Appointment!.Id);
        await Appointments.CreateAsync(patient.Id, doctor.Id, PastDate, Ten, "Check-up");

        Assert.Empty(Email.Sent);
    }

    [Fact]
    public async Task The_appointment_is_saved_even_when_the_email_cannot_be_sent()
    {
        var patient = await AddPatientAsync();
        var doctor = await AddDoctorAsync();
        Email.ShouldFail = true;

        var result = await Appointments.CreateAsync(patient.Id, doctor.Id, FutureDate, Ten, "Check-up");

        Assert.True(result.Succeeded);
        var saved = await Appointments.GetAsync(result.Appointment!.Id);
        Assert.NotNull(saved);
        Assert.Equal(AppointmentStatus.Scheduled, saved!.Status);
    }

    [Fact]
    public async Task Email_text_is_html_encoded()
    {
        var patient = await AddPatientAsync("<b>Eve</b>", "Example", "eve@example.com");
        var doctor = await AddDoctorAsync();

        await Appointments.CreateAsync(patient.Id, doctor.Id, FutureDate, Ten, "Check-up");

        var email = Assert.Single(Email.Sent);
        Assert.DoesNotContain("<b>Eve</b>", email.HtmlBody);
        Assert.Contains("&lt;b&gt;Eve&lt;/b&gt;", email.HtmlBody);
    }
}
