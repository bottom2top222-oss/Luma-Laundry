using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace LaundryApp.Controllers;

using LaundryApp.Models;

public class HomeController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return Redirect("/app/");
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

    private bool IsLoggedIn()
        => User?.Identity?.IsAuthenticated ?? false;
}

