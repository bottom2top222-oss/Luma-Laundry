using LaundryApp.Data;
using LaundryApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace LaundryApp.Services;

public class StripeBillingService
{
    private readonly LaundryAppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeBillingService> _logger;

    public StripeBillingService(
        LaundryAppDbContext db,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<StripeBillingService> logger)
    {
        _db = db;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_configuration["Stripe:SecretKey"]);

    public async Task<(string customerId, bool created)> EnsureCustomerAsync(ApplicationUser user)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Stripe is not configured. Set Stripe:SecretKey.");
        }

        if (!string.IsNullOrWhiteSpace(user.StripeCustomerId))
        {
            return (user.StripeCustomerId, false);
        }

        var customerService = new CustomerService();
        var customer = await customerService.CreateAsync(new CustomerCreateOptions
        {
            Email = user.Email,
            Name = string.Join(" ", new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim()
        });

        user.StripeCustomerId = customer.Id;
        await _userManager.UpdateAsync(user);
        return (customer.Id, true);
    }

    public async Task<SetupIntent> CreateSetupIntentAsync(ApplicationUser user)
    {
        var (customerId, _) = await EnsureCustomerAsync(user);

        var setupIntentService = new SetupIntentService();
        return await setupIntentService.CreateAsync(new SetupIntentCreateOptions
        {
            Customer = customerId,
            Usage = "off_session",
            PaymentMethodTypes = new List<string> { "card" }
        });
    }

    public async Task SetDefaultPaymentMethodAsync(string stripeCustomerId, string paymentMethodId)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.StripeCustomerId == stripeCustomerId);
        if (user == null)
        {
            _logger.LogWarning("No local user found for Stripe customer {CustomerId}", stripeCustomerId);
            return;
        }

        user.DefaultPaymentMethodId = paymentMethodId;
        await _db.SaveChangesAsync();

        var customerService = new CustomerService();
        await customerService.UpdateAsync(stripeCustomerId, new CustomerUpdateOptions
        {
            InvoiceSettings = new CustomerInvoiceSettingsOptions
            {
                DefaultPaymentMethod = paymentMethodId
            }
        });
    }

    public async Task<PaymentIntent> ChargeOrderAsync(int orderId)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Stripe is not configured. Set Stripe:SecretKey.");
        }

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null)
        {
            throw new InvalidOperationException("Order not found.");
        }

        var userEmail = (order.UserEmail ?? string.Empty).Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == userEmail || u.UserName == userEmail);
        if (user == null)
        {
            throw new InvalidOperationException("Customer account not found for order.");
        }

        if (string.IsNullOrWhiteSpace(user.StripeCustomerId) || string.IsNullOrWhiteSpace(user.DefaultPaymentMethodId))
        {
            throw new InvalidOperationException("No payment method on file.");
        }

        if (order.FinalAmountCents <= 0)
        {
            throw new InvalidOperationException("Final amount not set.");
        }

        var paymentIntentService = new PaymentIntentService();

        try
        {
            var pi = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = order.FinalAmountCents,
                Currency = string.IsNullOrWhiteSpace(order.Currency) ? "usd" : order.Currency,
                Customer = user.StripeCustomerId,
                PaymentMethod = user.DefaultPaymentMethodId,
                OffSession = true,
                Confirm = true,
                Description = $"Laundry Order {order.Id}",
                Metadata = new Dictionary<string, string>
                {
                    ["orderId"] = order.Id.ToString(),
                    ["userId"] = user.Id
                }
            });

            order.StripePaymentIntentId = pi.Id;
            order.Status = "ChargeAttempted";
            order.PaymentStatus = "ChargeAttempted";
            order.LastUpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return pi;
        }
        catch (StripeException ex)
        {
            order.Status = ex.StripeError?.Code == "authentication_required" ? "PaymentActionRequired" : "PaymentFailed";
            order.PaymentStatus = ex.StripeError?.Code == "authentication_required" ? "PaymentActionRequired" : "PaymentFailed";
            order.LastUpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            throw;
        }
    }

    public async Task HandlePaymentIntentSucceededAsync(PaymentIntent paymentIntent)
    {
        if (paymentIntent.Metadata == null || !paymentIntent.Metadata.TryGetValue("orderId", out var orderIdValue) || !int.TryParse(orderIdValue, out var orderId))
        {
            return;
        }

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return;

        order.StripePaymentIntentId = paymentIntent.Id;
        order.Status = "Paid";
        order.PaymentStatus = "Paid";
        order.LastUpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    public async Task HandlePaymentIntentFailedAsync(PaymentIntent paymentIntent)
    {
        if (paymentIntent.Metadata == null || !paymentIntent.Metadata.TryGetValue("orderId", out var orderIdValue) || !int.TryParse(orderIdValue, out var orderId))
        {
            return;
        }

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return;

        order.StripePaymentIntentId = paymentIntent.Id;
        order.Status = "PaymentFailed";
        order.PaymentStatus = "PaymentFailed";
        order.LastUpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
    }
}
