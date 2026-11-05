using HospitalManagement.Web.ViewModels;

namespace HospitalManagement.Web.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetAsync();
}
