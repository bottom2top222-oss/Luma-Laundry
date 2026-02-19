using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LaundryApp.Models;

namespace LaundryApp.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Email and password are required.");
            return View();
        }

        var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Dashboard", "Home");
        }

        ModelState.AddModelError("", "Invalid login attempt.");
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string email, string password, string confirmPassword, string firstName, string lastName, string phoneNumber, string addressLine1, string? addressLine2, string city, string state, string zipCode, string? acceptTerms)
    {
        var acceptedTerms = false;
        if (Request.HasFormContentType)
        {
            var submittedValues = Request.Form["acceptTerms"];
            acceptedTerms = submittedValues.Any(v =>
                string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(v, "on", StringComparison.OrdinalIgnoreCase));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError("", "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Password is required.");
        }

        if (password != confirmPassword)
        {
            ModelState.AddModelError("", "Passwords do not match.");
        }

        if (password != null && password.Length < 6)
        {
            ModelState.AddModelError("", "Password must be at least 6 characters long.");
        }

        if (!acceptedTerms)
        {
            ModelState.AddModelError("", "You must accept the Terms of Service to create an account.");
        }

        if (!ModelState.IsValid)
        {
            return View();
        }

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            ModelState.AddModelError("", "A user with this email already exists.");
            return View();
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            AddressLine1 = addressLine1?.Trim() ?? "",
            AddressLine2 = addressLine2?.Trim() ?? "",
            City = city?.Trim() ?? "",
            State = state?.Trim() ?? "",
            ZipCode = zipCode?.Trim() ?? "",
            TermsAccepted = acceptedTerms,
            TermsAcceptedAt = acceptedTerms ? DateTime.UtcNow.ToString("o") : ""
        };
        var result = await _userManager.CreateAsync(user, password!);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Dashboard", "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View();
    }

    [HttpGet]
    public IActionResult Terms()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }
}
