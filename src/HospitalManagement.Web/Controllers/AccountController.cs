using HospitalManagement.Web.Models;
using HospitalManagement.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(SignInManager<ApplicationUser> signInManager, ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Session cookie (not persistent). Too many wrong passwords lock the account for a while.
        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("A user signed in.");
            return !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
                ? LocalRedirect(model.ReturnUrl)
                : RedirectToAction("Index", "Home");
        }

        _logger.LogWarning("A sign-in attempt failed. LockedOut={LockedOut}", result.IsLockedOut);

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "This account is temporarily locked. Please try again later."
            : "Invalid email or password.");
        return View(model);
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
