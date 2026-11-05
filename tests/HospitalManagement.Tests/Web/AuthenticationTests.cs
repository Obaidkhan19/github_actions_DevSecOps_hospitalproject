using System.Net;

namespace HospitalManagement.Tests.Web;

[Collection("Web")]
public class AuthenticationTests
{
    private readonly TestWebApplicationFactory _factory;

    public AuthenticationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Anonymous_visitors_are_sent_to_the_login_page()
    {
        var client = WebTestHelpers.CreateBrowser(_factory);

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Login_page_is_available()
    {
        var client = WebTestHelpers.CreateBrowser(_factory);

        var response = await client.GetAsync("/Account/Login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_fails()
    {
        var client = WebTestHelpers.CreateBrowser(_factory);

        var response = await WebTestHelpers.LoginAsync(
            client, TestWebApplicationFactory.AdminEmail, "Wrong-Password-1!");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid email or password", html);
    }

    [Fact]
    public async Task Login_with_a_correct_password_opens_the_dashboard()
    {
        var client = await WebTestHelpers.LoggedInAsync(
            _factory, TestWebApplicationFactory.AdminEmail, TestWebApplicationFactory.AdminPassword);

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Dashboard", html);
        Assert.Contains("Test Hospital", html);
    }

    [Fact]
    public async Task Logout_ends_the_session()
    {
        var client = await WebTestHelpers.LoggedInAsync(
            _factory, TestWebApplicationFactory.AdminEmail, TestWebApplicationFactory.AdminPassword);

        var logout = await WebTestHelpers.PostFormAsync(
            client, "/Account/Logout", "/", new Dictionary<string, string>());
        var afterLogout = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, afterLogout.StatusCode);
        Assert.Contains("/Account/Login", afterLogout.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Forms_without_an_anti_forgery_token_are_rejected()
    {
        var client = WebTestHelpers.CreateBrowser(_factory);

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Email"] = TestWebApplicationFactory.AdminEmail,
                ["Password"] = TestWebApplicationFactory.AdminPassword
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
