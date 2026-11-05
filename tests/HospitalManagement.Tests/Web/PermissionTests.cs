using System.Net;

namespace HospitalManagement.Tests.Web;

[Collection("Web")]
public class PermissionTests
{
    private readonly TestWebApplicationFactory _factory;

    public PermissionTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private Task<HttpClient> AdminAsync() => WebTestHelpers.LoggedInAsync(
        _factory, TestWebApplicationFactory.AdminEmail, TestWebApplicationFactory.AdminPassword);

    private Task<HttpClient> StaffAsync() => WebTestHelpers.LoggedInAsync(
        _factory, TestWebApplicationFactory.StaffEmail, TestWebApplicationFactory.StaffPassword);

    [Theory]
    [InlineData("/Patients")]
    [InlineData("/Appointments")]
    [InlineData("/Doctors")]
    public async Task Staff_can_open_the_everyday_pages(string url)
    {
        var client = await StaffAsync();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/Users")]
    [InlineData("/Users/Create")]
    [InlineData("/Doctors/Create")]
    public async Task Staff_cannot_open_admin_pages(string url)
    {
        var client = await StaffAsync();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("AccessDenied", response.Headers.Location!.ToString());
    }

    [Theory]
    [InlineData("/Users")]
    [InlineData("/Users/Create")]
    [InlineData("/Doctors/Create")]
    public async Task Admin_can_open_admin_pages(string url)
    {
        var client = await AdminAsync();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_visitors_cannot_open_patient_pages()
    {
        var client = WebTestHelpers.CreateBrowser(_factory);

        var response = await client.GetAsync("/Patients");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Staff_cannot_delete_patients_but_admin_can()
    {
        var admin = await AdminAsync();
        var created = await WebTestHelpers.PostFormAsync(admin, "/Patients/Create", "/Patients/Create",
            new Dictionary<string, string>
            {
                ["FirstName"] = "Delete",
                ["LastName"] = "Candidate",
                ["DateOfBirth"] = "1990-05-20",
                ["Gender"] = "Other",
                ["Phone"] = "+49 000 0000000",
                ["Email"] = "delete.candidate@example.com",
                ["Address"] = "1 Sample Street"
            });
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var detailsUrl = created.Headers.Location!.ToString();
        var id = detailsUrl.Split('/').Last();

        var staff = await StaffAsync();
        var staffDelete = await WebTestHelpers.PostFormAsync(
            staff, $"/Patients/Delete/{id}", "/Patients", new Dictionary<string, string>());
        Assert.Equal(HttpStatusCode.Redirect, staffDelete.StatusCode);
        Assert.Contains("AccessDenied", staffDelete.Headers.Location!.ToString());

        var adminDelete = await WebTestHelpers.PostFormAsync(
            admin, $"/Patients/Delete/{id}", "/Patients", new Dictionary<string, string>());
        Assert.Equal(HttpStatusCode.Redirect, adminDelete.StatusCode);
        Assert.DoesNotContain("AccessDenied", adminDelete.Headers.Location!.ToString());
    }
}
