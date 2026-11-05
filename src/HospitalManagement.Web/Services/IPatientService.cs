using HospitalManagement.Web.Models;

namespace HospitalManagement.Web.Services;

public interface IPatientService
{
    Task<List<Patient>> SearchAsync(string? search);
    Task<Patient?> GetAsync(int id);
    Task<Patient> CreateAsync(Patient patient);
    Task<bool> UpdateAsync(Patient patient);
    Task<DeleteResult> DeleteAsync(int id);
}
