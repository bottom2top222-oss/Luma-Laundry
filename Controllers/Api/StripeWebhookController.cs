using LaundryApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace LaundryApp.Controllers.Api;

[ApiController]
[AllowAnonymous]
[Route("api/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly StripeBillingService _stripeBillingService;

    public StripeWebhookController(IConfiguration configuration, StripeBillingService stripeBillingService)
    {
        _configuration = configuration;
        _stripeBillingService = stripeBillingService;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signatureHeader = Request.Headers["Stripe-Signature"];
        var webhookSecret = _configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            return BadRequest("Stripe webhook secret is not configured.");
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, webhookSecret);
        }
        catch
        {
            return BadRequest();
        }

        if (stripeEvent.Type == "setup_intent.succeeded")
        {
            var setupIntent = stripeEvent.Data.Object as SetupIntent;
            if (setupIntent?.CustomerId != null && setupIntent.PaymentMethodId != null)
            {
                await _stripeBillingService.SetDefaultPaymentMethodAsync(setupIntent.CustomerId, setupIntent.PaymentMethodId);
            }
        }
        else if (stripeEvent.Type == "payment_intent.succeeded")
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent != null)
            {
                await _stripeBillingService.HandlePaymentIntentSucceededAsync(paymentIntent);
            }
        }
        else if (stripeEvent.Type == "payment_intent.payment_failed")
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent != null)
            {
                await _stripeBillingService.HandlePaymentIntentFailedAsync(paymentIntent);
            }
        }
        else if (stripeEvent.Type == "payment_intent.processing")
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent != null)
            {
                await _stripeBillingService.HandlePaymentIntentProcessingAsync(paymentIntent);
            }
        }
        else if (stripeEvent.Type == "payment_intent.requires_action")
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent != null)
            {
                await _stripeBillingService.HandlePaymentIntentRequiresActionAsync(paymentIntent);
            }
        }

        return Ok();
    }
}
