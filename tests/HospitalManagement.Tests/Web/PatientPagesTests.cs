using System.Net;

namespace HospitalManagement.Tests.Web;

[Collection("Web")]
public class PatientPagesTests
{
    private readonly TestWebApplicationFactory _factory;

    public PatientPagesTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Staff_can_register_view_and_edit_a_patient_through_the_website()
    {
        var client = await WebTestHelpers.LoggedInAsync(
            _factory, TestWebApplicationFactory.StaffEmail, TestWebApplicationFactory.StaffPassword);

        // Register
        var created = await WebTestHelpers.PostFormAsync(client, "/Patients/Create", "/Patients/Create",
            new Dictionary<string, string>
            {
                ["FirstName"] = "Integration",
                ["LastName"] = "Patient",
                ["DateOfBirth"] = "1990-05-20",
                ["Gender"] = "Female",
                ["Phone"] = "+49 000 0000000",
                ["Email"] = "integration.patient@example.com",
                ["Address"] = "1 Sample Street"
            });
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var detailsUrl = created.Headers.Location!.ToString();
        var id = detailsUrl.Split('/').Last();

        // View and search
        var details = await client.GetStringAsync(detailsUrl);
        Assert.Contains("Integration Patient", details);
        var search = await client.GetStringAsync("/Patients?search=Integration");
        Assert.Contains("integration.patient@example.com", search);

        // Edit
        var edited = await WebTestHelpers.PostFormAsync(client, $"/Patients/Edit/{id}", $"/Patients/Edit/{id}",
            new Dictionary<string, string>
            {
                ["Id"] = id,
                ["FirstName"] = "Integration",
                ["LastName"] = "Renamed",
                ["DateOfBirth"] = "1990-05-20",
                ["Gender"] = "Female",
                ["Phone"] = "+49 000 0000000",
                ["Email"] = "integration.patient@example.com",
                ["Address"] = "1 Sample Street"
            });
        Assert.Equal(HttpStatusCode.Redirect, edited.StatusCode);
        Assert.Contains("Integration Renamed", await client.GetStringAsync(detailsUrl));
    }

    [Fact]
    public async Task Invalid_patient_data_is_rejected_with_messages()
    {
        var client = await WebTestHelpers.LoggedInAsync(
            _factory, TestWebApplicationFactory.StaffEmail, TestWebApplicationFactory.StaffPassword);

        var response = await WebTestHelpers.PostFormAsync(client, "/Patients/Create", "/Patients/Create",
            new Dictionary<string, string>
            {
                ["FirstName"] = "",
                ["LastName"] = "Invalid",
                ["DateOfBirth"] = "2999-01-01",
                ["Gender"] = "Other",
                ["Phone"] = "+49 000 0000000",
                ["Email"] = "not-an-email",
                ["Address"] = "1 Sample Street"
            });
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("field-validation-error", html);
    }
}
