using HospitalManagement.Web.Models;

namespace HospitalManagement.Web.ViewModels;

public class DashboardViewModel
{
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int TodaysAppointmentCount { get; set; }
    public int UpcomingAppointmentCount { get; set; }
    public int CompletedAppointmentCount { get; set; }

    public List<Appointment> TodaysAppointments { get; set; } = new();
    public List<Patient> RecentPatients { get; set; } = new();
}
