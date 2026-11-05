using HospitalManagement.Web.Authentication;
using HospitalManagement.Web.Data;
using HospitalManagement.Web.Models;
using HospitalManagement.Web.Services.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HospitalManagement.Tests.Web;

/// <summary>
/// Starts the real application in memory, with a temporary SQLite database.
/// It creates one Admin and one Staff user for the tests.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@example.com";
    public const string AdminPassword = "Admin-Test-123!";
    public const string StaffEmail = "staff@example.com";
    public const string StaffPassword = "Staff-Test-123!";

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"hospital-tests-{Guid.NewGuid():N}.db");

    public TestWebApplicationFactory()
    {
        // Environment variables are read when the application starts, so they are reliable.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("Hospital__Name", "Test Hospital");
        Environment.SetEnvironmentVariable("Seed__AdminEmail", AdminEmail);
        Environment.SetEnvironmentVariable("Seed__AdminPassword", AdminPassword);
        Environment.SetEnvironmentVariable("Seed__AdminName", "Test Admin");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"DataSource={_databasePath}"));

            // No real emails in tests.
            services.AddSingleton<IEmailService, FakeEmail>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();

        DatabaseInitializer.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var staff = new ApplicationUser
        {
            UserName = StaffEmail,
            Email = StaffEmail,
            FullName = "Test Staff",
            EmailConfirmed = true
        };
        users.CreateAsync(staff, StaffPassword).GetAwaiter().GetResult();
        users.AddToRoleAsync(staff, AppRoles.Staff).GetAwaiter().GetResult();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            SqliteConnection.ClearAllPools();
            try
            {
                File.Delete(_databasePath);
            }
            catch (IOException)
            {
                // A temporary file that cannot be removed is not a test failure.
            }
        }
    }

    private sealed class FakeEmail : IEmailService
    {
        public Task SendAsync(string to, string subject, string htmlBody) => Task.CompletedTask;
    }
}

/// <summary>All web tests share one application instance.</summary>
[CollectionDefinition("Web")]
public class WebCollection : ICollectionFixture<TestWebApplicationFactory>
{
}
