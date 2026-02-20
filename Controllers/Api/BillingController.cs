using LaundryApp.Data;
using LaundryApp.Models;
using LaundryApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LaundryApp.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/billing")]
public class BillingController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly StripeBillingService _stripeBillingService;

    public BillingController(UserManager<ApplicationUser> userManager, StripeBillingService stripeBillingService)
    {
        _userManager = userManager;
        _stripeBillingService = stripeBillingService;
    }

    [HttpPost("customer")]
    public async Task<IActionResult> CreateOrFetchCustomer()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var (customerId, created) = await _stripeBillingService.EnsureCustomerAsync(user);
        return Ok(new { stripeCustomerId = customerId, created });
    }

    [HttpPost("setup-intent")]
    public async Task<IActionResult> CreateSetupIntent()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var setupIntent = await _stripeBillingService.CreateSetupIntentAsync(user);
        return Ok(new { clientSecret = setupIntent.ClientSecret, setupIntentId = setupIntent.Id });
    }
}
