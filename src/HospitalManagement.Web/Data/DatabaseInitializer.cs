using HospitalManagement.Web.Authentication;
using HospitalManagement.Web.Configuration;
using HospitalManagement.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HospitalManagement.Web.Data;

/// <summary>
/// Applies migrations (optional) and creates the first data: roles, the first admin
/// user and, if requested, synthetic sample data. Safe to run again and again.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, bool migrate)
    {
        using var scope = services.CreateScope();

        if (migrate)
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
        }

        await SeedAsync(scope.ServiceProvider);
    }

    public static async Task SeedAsync(IServiceProvider scopedServices)
    {
        var db = scopedServices.GetRequiredService<ApplicationDbContext>();
        var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();
        var seed = scopedServices.GetRequiredService<IOptions<SeedOptions>>().Value;

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        if (!string.IsNullOrWhiteSpace(seed.AdminEmail) &&
            !string.IsNullOrWhiteSpace(seed.AdminPassword) &&
            !await userManager.Users.AnyAsync())
        {
            var admin = new ApplicationUser
            {
                UserName = seed.AdminEmail,
                Email = seed.AdminEmail,
                FullName = seed.AdminName,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, seed.AdminPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Could not create the first admin user: {errors}");
            }

            await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }

        if (seed.SampleData)
        {
            await SeedSampleDataAsync(db);
        }
    }

    // Synthetic data only. These people do not exist.
    private static async Task SeedSampleDataAsync(ApplicationDbContext db)
    {
        if (!await db.Doctors.AnyAsync())
        {
            db.Doctors.AddRange(
                new Doctor { FirstName = "Anna", LastName = "Becker", Specialization = "Cardiology", Department = "Heart Centre" },
                new Doctor { FirstName = "Markus", LastName = "Weber", Specialization = "General Medicine", Department = "Outpatient" },
                new Doctor { FirstName = "Laura", LastName = "Fischer", Specialization = "Pediatrics", Department = "Children's Ward" },
                new Doctor { FirstName = "Tom", LastName = "Keller", Specialization = "Orthopedics", Department = "Surgery" });
        }

        if (!await db.Patients.AnyAsync())
        {
            var now = DateTime.UtcNow;
            db.Patients.AddRange(
                NewPatient("Max", "Mustermann", new DateOnly(1985, 3, 14), "Male", "max.mustermann@example.com", now),
                NewPatient("Erika", "Musterfrau", new DateOnly(1990, 7, 2), "Female", "erika.musterfrau@example.com", now),
                NewPatient("John", "Doe", new DateOnly(1978, 11, 23), "Male", "john.doe@example.com", now),
                NewPatient("Jane", "Roe", new DateOnly(2001, 1, 30), "Female", "jane.roe@example.com", now),
                NewPatient("Sam", "Sample", new DateOnly(1969, 9, 9), "Other", "sam.sample@example.com", now));
        }

        await db.SaveChangesAsync();
    }

    private static Patient NewPatient(
        string first, string last, DateOnly dateOfBirth, string gender, string email, DateTime now) =>
        new()
        {
            FirstName = first,
            LastName = last,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            Phone = "+49 000 0000000",
            Email = email,
            Address = "1 Sample Street, Sampletown",
            RegistrationDate = now
        };
}
