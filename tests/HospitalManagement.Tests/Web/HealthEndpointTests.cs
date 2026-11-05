using System.Net;

namespace HospitalManagement.Tests.Web;

[Collection("Web")]
public class HealthEndpointTests
{
    private readonly TestWebApplicationFactory _factory;

    public HealthEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy_when_the_database_works()
    {
        var client = WebTestHelpers.CreateBrowser(_factory);

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_endpoint_needs_no_login()
    {
        var client = WebTestHelpers.CreateBrowser(_factory);

        var response = await client.GetAsync("/health");

        Assert.NotEqual(HttpStatusCode.Redirect, response.StatusCode);
    }
}
