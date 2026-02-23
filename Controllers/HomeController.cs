using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LaundryApp.Controllers;

using LaundryApp.Models;
using LaundryApp.Services;

public class HomeController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ServiceAreaOptions _serviceAreaOptions;

    public HomeController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IOptions<ServiceAreaOptions> serviceAreaOptions)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _serviceAreaOptions = serviceAreaOptions.Value;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html"), "text/html");
    }

    [HttpGet]
    public IActionResult Dashboard()
    {
        if (!IsLoggedIn())
            return RedirectToAction("Index");

        ViewData["Title"] = "Dashboard";
        return View();
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Pricing()
    {
        ViewData["Title"] = "Pricing";
        return View();
    }

    [HttpGet]
    public IActionResult HowItWorks()
    {
        ViewData["Title"] = "How It Works";
        return View();
    }

    [HttpGet]
    public IActionResult FirstOrder()
    {
        ViewData["Title"] = "First Order Guide";
        return View();
    }

    [HttpGet]
    public IActionResult Locations(string? city = null)
    {
        ViewData["Title"] = "Locations";

        var serviceLocations = _serviceAreaOptions.AllowedCities
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();

        ViewData["ServiceLocations"] = serviceLocations;
        ViewData["ServiceState"] = string.IsNullOrWhiteSpace(_serviceAreaOptions.AllowedState)
            ? "FL"
            : _serviceAreaOptions.AllowedState.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(city))
        {
            var selectedLocation = serviceLocations
                .FirstOrDefault(c => string.Equals(c, city.Trim(), StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(selectedLocation))
            {
                ViewData["SelectedLocation"] = selectedLocation;
            }
        }

        return View();
    }

    private bool IsLoggedIn()
        => User?.Identity?.IsAuthenticated ?? false;
}

