using HospitalManagement.Web.Data;
using HospitalManagement.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Web.Services;

public class DoctorService : IDoctorService
{
    private readonly ApplicationDbContext _db;

    public DoctorService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<List<Doctor>> ListAsync() =>
        _db.Doctors
            .AsNoTracking()
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .ToListAsync();

    public Task<Doctor?> GetAsync(int id) =>
        _db.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);

    public async Task<Doctor> CreateAsync(Doctor doctor)
    {
        doctor.Id = 0;
        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();
        return doctor;
    }

    public async Task<bool> UpdateAsync(Doctor doctor)
    {
        var existing = await _db.Doctors.FindAsync(doctor.Id);
        if (existing is null)
        {
            return false;
        }

        existing.FirstName = doctor.FirstName;
        existing.LastName = doctor.LastName;
        existing.Specialization = doctor.Specialization;
        existing.Department = doctor.Department;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<DeleteResult> DeleteAsync(int id)
    {
        var doctor = await _db.Doctors.FindAsync(id);
        if (doctor is null)
        {
            return DeleteResult.NotFound;
        }

        if (await _db.Appointments.AnyAsync(a => a.DoctorId == id))
        {
            return DeleteResult.InUse;
        }

        _db.Doctors.Remove(doctor);
        await _db.SaveChangesAsync();
        return DeleteResult.Deleted;
    }
}
