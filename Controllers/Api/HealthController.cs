using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryApp.Controllers.Api;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public HealthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("stripe")]
    public IActionResult Stripe()
    {
        var publishableConfigured = !string.IsNullOrWhiteSpace(_configuration["Stripe:PublishableKey"]?.Trim());
        var secretConfigured = !string.IsNullOrWhiteSpace(_configuration["Stripe:SecretKey"]?.Trim());
        var webhookConfigured = !string.IsNullOrWhiteSpace(_configuration["Stripe:WebhookSecret"]?.Trim());

        return Ok(new
        {
            stripeConfigured = publishableConfigured && secretConfigured,
            publishableConfigured,
            secretConfigured,
            webhookConfigured,
            environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown"
        });
    }
}