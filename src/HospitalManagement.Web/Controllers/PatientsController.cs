using HospitalManagement.Web.Authentication;
using HospitalManagement.Web.Models;
using HospitalManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Web.Controllers;

[Authorize(Roles = AppRoles.AdminOrStaff)]
public class PatientsController : Controller
{
    private const string EditableFields = "Id,FirstName,LastName,DateOfBirth,Gender,Phone,Email,Address";

    private readonly IPatientService _patients;

    public PatientsController(IPatientService patients)
    {
        _patients = patients;
    }

    public async Task<IActionResult> Index(string? search)
    {
        ViewData["Search"] = search;
        return View(await _patients.SearchAsync(search));
    }

    public async Task<IActionResult> Details(int id)
    {
        var patient = await _patients.GetAsync(id);
        return patient is null ? NotFound() : View(patient);
    }

    [HttpGet]
    public IActionResult Create() => View(new Patient());

    [HttpPost]
    public async Task<IActionResult> Create([Bind(EditableFields)] Patient patient)
    {
        if (!ModelState.IsValid)
        {
            return View(patient);
        }

        var created = await _patients.CreateAsync(patient);
        TempData["Success"] = "Patient registered.";
        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var patient = await _patients.GetAsync(id);
        return patient is null ? NotFound() : View(patient);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, [Bind(EditableFields)] Patient patient)
    {
        if (id != patient.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(patient);
        }

        if (!await _patients.UpdateAsync(patient))
        {
            return NotFound();
        }

        TempData["Success"] = "Patient updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _patients.DeleteAsync(id);

        switch (result)
        {
            case DeleteResult.Deleted:
                TempData["Success"] = "Patient deleted.";
                break;
            case DeleteResult.InUse:
                TempData["Error"] = "This patient has appointments and cannot be deleted.";
                break;
            default:
                return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }
}
