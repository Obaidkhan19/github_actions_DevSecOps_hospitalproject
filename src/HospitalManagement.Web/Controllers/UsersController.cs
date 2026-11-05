using HospitalManagement.Web.Authentication;
using HospitalManagement.Web.Models;
using HospitalManagement.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Web.Controllers;

/// <summary>Admin only: manage the staff who can sign in.</summary>
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();
        var items = new List<UserListItem>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(new UserListItem
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Role = string.Join(", ", roles)
            });
        }

        return View(items);
    }

    [HttpGet]
    public IActionResult Create() => View(new UserFormViewModel { Role = AppRoles.Staff });

    [HttpPost]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "A password is required.");
        }

        if (!AppRoles.All.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Choose Admin or Staff.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password!);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, model.Role);
        TempData["Success"] = "User created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return View(new UserFormViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Role = roles.FirstOrDefault() ?? AppRoles.Staff
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(string id, UserFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (!AppRoles.All.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Choose Admin or Staff.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(model.Role) && await IsLastAdminAsync(user))
        {
            ModelState.AddModelError(nameof(model.Role), "The last Admin cannot lose the Admin role.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        user.FullName = model.FullName;
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            AddErrors(update);
            return View(model);
        }

        if (!currentRoles.Contains(model.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        // An optional new password. Leave it empty to keep the current one.
        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (!reset.Succeeded)
            {
                AddErrors(reset);
                return View(model);
            }
        }

        TempData["Success"] = "User updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        if (await IsLastAdminAsync(user))
        {
            TempData["Error"] = "The last Admin cannot be deleted.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.DeleteAsync(user);
        TempData["Success"] = "User deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> IsLastAdminAsync(ApplicationUser user)
    {
        if (!await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            return false;
        }

        var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
        return admins.Count <= 1;
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
