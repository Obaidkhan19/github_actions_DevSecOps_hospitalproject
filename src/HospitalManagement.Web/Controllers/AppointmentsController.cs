using HospitalManagement.Web.Authentication;
using HospitalManagement.Web.Models;
using HospitalManagement.Web.Services;
using HospitalManagement.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalManagement.Web.Controllers;

[Authorize(Roles = AppRoles.AdminOrStaff)]
public class AppointmentsController : Controller
{
    private readonly IAppointmentService _appointments;
    private readonly IPatientService _patients;
    private readonly IDoctorService _doctors;
    private readonly TimeProvider _time;

    public AppointmentsController(
        IAppointmentService appointments,
        IPatientService patients,
        IDoctorService doctors,
        TimeProvider time)
    {
        _appointments = appointments;
        _patients = patients;
        _doctors = doctors;
        _time = time;
    }

    public async Task<IActionResult> Index(string? search, AppointmentStatus? status, DateOnly? date)
    {
        var model = new AppointmentIndexViewModel
        {
            Search = search,
            Status = status,
            Date = date,
            Appointments = await _appointments.SearchAsync(search, status, date)
        };
        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var appointment = await _appointments.GetAsync(id);
        return appointment is null ? NotFound() : View(appointment);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? patientId)
    {
        var model = new AppointmentFormViewModel
        {
            PatientId = patientId ?? 0,
            Date = DateOnly.FromDateTime(_time.GetLocalNow().DateTime),
            Time = new TimeOnly(9, 0)
        };
        await FillListsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AppointmentFormViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _appointments.CreateAsync(
                model.PatientId, model.DoctorId, model.Date, model.Time, model.Reason);

            if (result.Succeeded)
            {
                TempData["Success"] = "Appointment scheduled.";
                return RedirectToAction(nameof(Details), new { id = result.Appointment!.Id });
            }

            ModelState.AddModelError(string.Empty, result.Error!);
        }

        await FillListsAsync(model);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var appointment = await _appointments.GetAsync(id);
        if (appointment is null)
        {
            return NotFound();
        }

        if (appointment.Status != AppointmentStatus.Scheduled)
        {
            TempData["Error"] = "Only scheduled appointments can be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new AppointmentEditViewModel
        {
            Id = appointment.Id,
            PatientName = appointment.Patient!.FullName,
            DoctorId = appointment.DoctorId,
            Reason = appointment.Reason
        };
        model.Doctors = await DoctorItemsAsync();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, AppointmentEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (ModelState.IsValid)
        {
            var result = await _appointments.UpdateDetailsAsync(id, model.DoctorId, model.Reason);
            if (result.Succeeded)
            {
                TempData["Success"] = "Appointment updated.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ModelState.AddModelError(string.Empty, result.Error!);
        }

        model.Doctors = await DoctorItemsAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Reschedule(int id)
    {
        var appointment = await _appointments.GetAsync(id);
        if (appointment is null)
        {
            return NotFound();
        }

        if (appointment.Status != AppointmentStatus.Scheduled)
        {
            TempData["Error"] = "Only scheduled appointments can be rescheduled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(new RescheduleViewModel
        {
            Id = appointment.Id,
            PatientName = appointment.Patient!.FullName,
            DoctorName = appointment.Doctor!.DisplayName,
            Date = appointment.AppointmentDate,
            Time = appointment.AppointmentTime
        });
    }

    [HttpPost]
    public async Task<IActionResult> Reschedule(int id, RescheduleViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (ModelState.IsValid)
        {
            var result = await _appointments.RescheduleAsync(id, model.Date, model.Time);
            if (result.Succeeded)
            {
                TempData["Success"] = "Appointment rescheduled.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ModelState.AddModelError(string.Empty, result.Error!);
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _appointments.CancelAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Appointment cancelled." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Complete(int id)
    {
        var result = await _appointments.CompleteAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Appointment marked as completed." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task FillListsAsync(AppointmentFormViewModel model)
    {
        var patients = await _patients.SearchAsync(null);
        model.Patients = patients
            .Select(p => new SelectListItem($"{p.LastName}, {p.FirstName} (#{p.Id})", p.Id.ToString()))
            .ToList();
        model.Doctors = await DoctorItemsAsync();
    }

    private async Task<List<SelectListItem>> DoctorItemsAsync()
    {
        var doctors = await _doctors.ListAsync();
        return doctors
            .Select(d => new SelectListItem($"{d.DisplayName} - {d.Specialization}", d.Id.ToString()))
            .ToList();
    }
}
