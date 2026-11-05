using HospitalManagement.Web.Models;

namespace HospitalManagement.Web.Services;

public interface IDoctorService
{
    Task<List<Doctor>> ListAsync();
    Task<Doctor?> GetAsync(int id);
    Task<Doctor> CreateAsync(Doctor doctor);
    Task<bool> UpdateAsync(Doctor doctor);
    Task<DeleteResult> DeleteAsync(int id);
}
