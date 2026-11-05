using HospitalManagement.Web.Data;
using HospitalManagement.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Web.Services;

public class PatientService : IPatientService
{
    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _time;

    public PatientService(ApplicationDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<List<Patient>> SearchAsync(string? search)
    {
        IQueryable<Patient> query = _db.Patients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var idTerm = int.TryParse(term, out var parsed) ? parsed : -1;

            query = query.Where(p =>
                p.FirstName.Contains(term) ||
                p.LastName.Contains(term) ||
                (p.FirstName + " " + p.LastName).Contains(term) ||
                p.Email.Contains(term) ||
                p.Phone.Contains(term) ||
                p.Id == idTerm);
        }

        return await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();
    }

    public Task<Patient?> GetAsync(int id) =>
        _db.Patients
            .AsNoTracking()
            .Include(p => p.Appointments)
            .ThenInclude(a => a.Doctor)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Patient> CreateAsync(Patient patient)
    {
        patient.Id = 0;
        patient.RegistrationDate = _time.GetUtcNow().UtcDateTime;

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        return patient;
    }

    public async Task<bool> UpdateAsync(Patient patient)
    {
        var existing = await _db.Patients.FindAsync(patient.Id);
        if (existing is null)
        {
            return false;
        }

        existing.FirstName = patient.FirstName;
        existing.LastName = patient.LastName;
        existing.DateOfBirth = patient.DateOfBirth;
        existing.Gender = patient.Gender;
        existing.Phone = patient.Phone;
        existing.Email = patient.Email;
        existing.Address = patient.Address;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<DeleteResult> DeleteAsync(int id)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient is null)
        {
            return DeleteResult.NotFound;
        }

        if (await _db.Appointments.AnyAsync(a => a.PatientId == id))
        {
            return DeleteResult.InUse;
        }

        _db.Patients.Remove(patient);
        await _db.SaveChangesAsync();
        return DeleteResult.Deleted;
    }
}
