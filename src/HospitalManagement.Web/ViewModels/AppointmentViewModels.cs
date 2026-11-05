using System.ComponentModel.DataAnnotations;
using HospitalManagement.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalManagement.Web.ViewModels;

public class AppointmentIndexViewModel
{
    public string? Search { get; set; }
    public AppointmentStatus? Status { get; set; }
    public DateOnly? Date { get; set; }
    public List<Appointment> Appointments { get; set; } = new();
}

public class AppointmentFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Please select a patient.")]
    [Display(Name = "Patient")]
    public int PatientId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a doctor.")]
    [Display(Name = "Doctor")]
    public int DoctorId { get; set; }

    [DataType(DataType.Date)]
    public DateOnly Date { get; set; }

    [DataType(DataType.Time)]
    public TimeOnly Time { get; set; }

    [Required, StringLength(500)]
    [Display(Name = "Reason for appointment")]
    public string Reason { get; set; } = string.Empty;

    public List<SelectListItem> Patients { get; set; } = new();
    public List<SelectListItem> Doctors { get; set; } = new();
}

public class AppointmentEditViewModel
{
    public int Id { get; set; }

    public string PatientName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Please select a doctor.")]
    [Display(Name = "Doctor")]
    public int DoctorId { get; set; }

    [Required, StringLength(500)]
    [Display(Name = "Reason for appointment")]
    public string Reason { get; set; } = string.Empty;

    public List<SelectListItem> Doctors { get; set; } = new();
}

public class RescheduleViewModel
{
    public int Id { get; set; }

    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "New date")]
    public DateOnly Date { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "New time")]
    public TimeOnly Time { get; set; }
}
