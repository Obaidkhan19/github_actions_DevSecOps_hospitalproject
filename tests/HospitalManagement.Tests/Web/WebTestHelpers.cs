using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HospitalManagement.Tests.Web;

public static class WebTestHelpers
{
    public static HttpClient CreateBrowser(TestWebApplicationFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    /// <summary>Opens a page and reads the hidden anti-forgery token from its form.</summary>
    public static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        var match = Regex.Match(
            html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");

        Assert.True(match.Success, $"No anti-forgery token found on {url}");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    public static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string url, string tokenPage, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = await GetAntiforgeryTokenAsync(client, tokenPage);
        return await client.PostAsync(url, new FormUrlEncodedContent(fields));
    }

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        PostFormAsync(client, "/Account/Login", "/Account/Login", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password
        });

    public static async Task<HttpClient> LoggedInAsync(
        TestWebApplicationFactory factory, string email, string password)
    {
        var client = CreateBrowser(factory);
        var response = await LoginAsync(client, email, password);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }
}
