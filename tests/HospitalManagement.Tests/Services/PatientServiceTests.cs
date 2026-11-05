using HospitalManagement.Tests.Helpers;
using HospitalManagement.Web.Services;

namespace HospitalManagement.Tests.Services;

public class PatientServiceTests : ServiceTestBase
{
    [Fact]
    public async Task Create_saves_the_patient_with_registration_date_and_timestamps()
    {
        var patient = await AddPatientAsync();

        Assert.True(patient.Id > 0);
        Assert.Equal(Time.GetUtcNow().UtcDateTime, patient.RegistrationDate);
        Assert.NotEqual(default, patient.CreatedAt);
        Assert.NotEqual(default, patient.UpdatedAt);
    }

    [Fact]
    public async Task Get_returns_the_patient()
    {
        var created = await AddPatientAsync(first: "Max", last: "Mustermann");

        var found = await Patients.GetAsync(created.Id);

        Assert.NotNull(found);
        Assert.Equal("Max Mustermann", found!.FullName);
    }

    [Fact]
    public async Task Update_changes_the_patient()
    {
        var created = await AddPatientAsync();
        created.Phone = "+49 111 1111111";
        created.Address = "2 New Street, Newtown";

        var updated = await Patients.UpdateAsync(created);
        var found = await Patients.GetAsync(created.Id);

        Assert.True(updated);
        Assert.Equal("+49 111 1111111", found!.Phone);
        Assert.Equal("2 New Street, Newtown", found.Address);
    }

    [Fact]
    public async Task Update_of_a_missing_patient_returns_false()
    {
        var missing = new HospitalManagement.Web.Models.Patient { Id = 999 };

        Assert.False(await Patients.UpdateAsync(missing));
    }

    [Fact]
    public async Task Search_finds_patients_by_name_and_email()
    {
        await AddPatientAsync("Max", "Mustermann", "max@example.com");
        await AddPatientAsync("Jane", "Roe", "jane@example.com");

        var byName = await Patients.SearchAsync("must");
        var byEmail = await Patients.SearchAsync("jane@");
        var all = await Patients.SearchAsync(null);

        Assert.Single(byName);
        Assert.Equal("Mustermann", byName[0].LastName);
        Assert.Single(byEmail);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task Delete_removes_a_patient_without_appointments()
    {
        var patient = await AddPatientAsync();

        var result = await Patients.DeleteAsync(patient.Id);

        Assert.Equal(DeleteResult.Deleted, result);
        Assert.Null(await Patients.GetAsync(patient.Id));
    }

    [Fact]
    public async Task Delete_is_blocked_when_the_patient_has_appointments()
    {
        var patient = await AddPatientAsync();
        var doctor = await AddDoctorAsync();
        await Appointments.CreateAsync(patient.Id, doctor.Id, FutureDate, new TimeOnly(10, 0), "Check-up");

        var result = await Patients.DeleteAsync(patient.Id);

        Assert.Equal(DeleteResult.InUse, result);
        Assert.NotNull(await Patients.GetAsync(patient.Id));
    }

    [Fact]
    public async Task Delete_of_a_missing_patient_reports_not_found()
    {
        Assert.Equal(DeleteResult.NotFound, await Patients.DeleteAsync(12345));
    }
}
