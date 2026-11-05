using HospitalManagement.Web.Authentication;
using HospitalManagement.Web.Models;
using HospitalManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Web.Controllers;

[Authorize(Roles = AppRoles.AdminOrStaff)]
public class DoctorsController : Controller
{
    private const string EditableFields = "Id,FirstName,LastName,Specialization,Department";

    private readonly IDoctorService _doctors;

    public DoctorsController(IDoctorService doctors)
    {
        _doctors = doctors;
    }

    // Staff can view doctors. Only an Admin can add, edit or delete them.
    public async Task<IActionResult> Index() => View(await _doctors.ListAsync());

    [HttpGet, Authorize(Roles = AppRoles.Admin)]
    public IActionResult Create() => View(new Doctor());

    [HttpPost, Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create([Bind(EditableFields)] Doctor doctor)
    {
        if (!ModelState.IsValid)
        {
            return View(doctor);
        }

        await _doctors.CreateAsync(doctor);
        TempData["Success"] = "Doctor added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var doctor = await _doctors.GetAsync(id);
        return doctor is null ? NotFound() : View(doctor);
    }

    [HttpPost, Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id, [Bind(EditableFields)] Doctor doctor)
    {
        if (id != doctor.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(doctor);
        }

        if (!await _doctors.UpdateAsync(doctor))
        {
            return NotFound();
        }

        TempData["Success"] = "Doctor updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _doctors.DeleteAsync(id);

        switch (result)
        {
            case DeleteResult.Deleted:
                TempData["Success"] = "Doctor deleted.";
                break;
            case DeleteResult.InUse:
                TempData["Error"] = "This doctor has appointments and cannot be deleted.";
                break;
            default:
                return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }
}
