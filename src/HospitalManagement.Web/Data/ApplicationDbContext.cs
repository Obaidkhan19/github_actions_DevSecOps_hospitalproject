using HospitalManagement.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Web.Data;

/// <summary>
/// The database of one hospital. Tables: Users (and the Identity role tables),
/// Patients, Doctors and Appointments.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // The Identity user table is called "Users". Its user name is the email address,
        // and Identity already enforces a unique user name.
        builder.Entity<ApplicationUser>().ToTable("Users");

        builder.Entity<Patient>(entity =>
        {
            entity.HasIndex(p => new { p.LastName, p.FirstName });
            entity.HasIndex(p => p.Email);
        });

        builder.Entity<Doctor>(entity =>
        {
            entity.HasIndex(d => new { d.LastName, d.FirstName });
        });

        builder.Entity<Appointment>(entity =>
        {
            // A patient or doctor with appointments cannot be deleted by accident.
            entity.HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(a => a.Status).HasConversion<int>();

            entity.HasIndex(a => new { a.AppointmentDate, a.Status });

            // The database itself refuses two active appointments for the same doctor
            // at the same date and time. Status 2 = Cancelled, which is ignored.
            entity.HasIndex(a => new { a.DoctorId, a.AppointmentDate, a.AppointmentTime })
                .IsUnique()
                .HasFilter("[Status] <> 2");
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<ITimestamped>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
