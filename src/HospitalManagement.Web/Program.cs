using HospitalManagement.Web.Configuration;
using HospitalManagement.Web.Data;
using HospitalManagement.Web.Models;
using HospitalManagement.Web.Services;
using HospitalManagement.Web.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ---------- Logging ----------
// Structured (JSON) logs on the console. Containers write logs to the console,
// and log tools can read JSON easily.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

// ---------- Configuration (all from environment variables or appsettings) ----------
builder.Services
    .AddOptions<HospitalOptions>()
    .Bind(builder.Configuration.GetSection(HospitalOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));

// ---------- Database ----------
// The automated tests run in the "Testing" environment and register their own database.
if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Database connection string is missing. Set the environment variable " +
            "ConnectionStrings__DefaultConnection.");
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// ---------- Authentication and security ----------
var secureCookies = builder.Configuration.GetValue<bool>("Security:SecureCookies");
var cookiePolicy = secureCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = ".HospitalManagement.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = cookiePolicy;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = cookiePolicy;
});

// ---------- Application services ----------
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAppointmentNotifier, AppointmentNotifier>();
builder.Services.AddScoped<IEmailService>(serviceProvider =>
{
    var email = serviceProvider.GetRequiredService<IOptions<EmailOptions>>().Value;
    if (email.Enabled && !string.IsNullOrWhiteSpace(email.Host))
    {
        return ActivatorUtilities.CreateInstance<SmtpEmailService>(serviceProvider);
    }

    return ActivatorUtilities.CreateInstance<NoOpEmailService>(serviceProvider);
});

// ---------- Health and web ----------
// /health reports Unhealthy when the database cannot be reached.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

builder.Services.AddControllersWithViews(options =>
{
    // CSRF protection for every form submission (POST, PUT, DELETE).
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Optional: apply database migrations when the app starts (Database__MigrateOnStartup=true).
// Migrations are repeatable: already applied ones are skipped.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await DatabaseInitializer.InitializeAsync(app.Services, migrate: true);
}

app.Run();

// Makes the Program class visible to the test project.
public partial class Program { }
