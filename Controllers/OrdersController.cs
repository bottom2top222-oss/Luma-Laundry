using LaundryApp.Data;
using LaundryApp.Models;
using LaundryApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;

namespace LaundryApp.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private const string SavePaymentTermsSessionPrefix = "save-payment-terms:";

    private readonly OrderStore _orderStore;
    private readonly LaundryAppDbContext _context;
    private readonly PaymentService _paymentService;
    private readonly StripeBillingService _stripeBillingService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly LayeredApiJobClient _layeredApiJobClient;
    private readonly LayeredApiOrderClient _layeredApiOrderClient;
    private readonly IConfiguration _configuration;
    private readonly bool _apiOnlyMode;

    public OrdersController(OrderStore orderStore, LaundryAppDbContext context, PaymentService paymentService, StripeBillingService stripeBillingService, UserManager<ApplicationUser> userManager, LayeredApiJobClient layeredApiJobClient, LayeredApiOrderClient layeredApiOrderClient, IConfiguration configuration)
    {
        _orderStore = orderStore;
        _context = context;
        _paymentService = paymentService;
        _stripeBillingService = stripeBillingService;
        _userManager = userManager;
        _layeredApiJobClient = layeredApiJobClient;
        _layeredApiOrderClient = layeredApiOrderClient;
        _configuration = configuration;
        _apiOnlyMode = bool.TryParse(configuration["LayeredServices:ApiOnlyMode"], out var apiOnly) && apiOnly;
    }

    [HttpGet]
    public IActionResult Schedule()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Schedule(string serviceType, DateTime scheduledAt, string addressLine1, string? addressLine2, string city, string state, string zipCode, string? notes)
    {
        if (string.IsNullOrWhiteSpace(serviceType))
            ModelState.AddModelError("", "Choose an order type.");

        if (scheduledAt == default)
            ModelState.AddModelError("", "Choose a date and time.");

        if (string.IsNullOrWhiteSpace(addressLine1))
            ModelState.AddModelError("", "Enter a street address.");

        if (string.IsNullOrWhiteSpace(city))
            ModelState.AddModelError("", "Enter a city.");

        if (string.IsNullOrWhiteSpace(state))
            ModelState.AddModelError("", "Enter a state.");

        if (string.IsNullOrWhiteSpace(zipCode))
            ModelState.AddModelError("", "Enter a ZIP code.");

        if (!ModelState.IsValid)
            return View();

        var order = new LaundryOrder
        {
            ServiceType = serviceType,
            ScheduledAt = scheduledAt,
            AddressLine1 = addressLine1?.Trim() ?? "",
            AddressLine2 = addressLine2?.Trim() ?? "",
            City = city?.Trim() ?? "",
            State = state?.Trim() ?? "",
            ZipCode = zipCode?.Trim() ?? "",
            Notes = notes?.Trim() ?? "",
            Status = "PendingPickup",
            PaymentStatus = "NoPaymentMethod",
            UserEmail = (User?.Identity?.Name ?? "").Trim()
        };

        order.Address = order.GetDisplayAddress();

        var apiOrderId = await _layeredApiOrderClient.CreateOrderAsync(order);

        if (apiOrderId.HasValue)
        {
            order.Id = apiOrderId.Value;
        }
        else
        {
            order = _orderStore.Add(order);
        }

        await _layeredApiJobClient.QueueOrderCreatedEmailAsync(order);

        // Redirect to payment method setup instead of payment
        return RedirectToAction("SavePaymentMethod", new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> SavePaymentMethod(int id)
    {
        var order = await GetOrderAsync(id);
        if (order == null) return NotFound();

        // Check if user owns this order
        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        ViewBag.StripeConfigured = _stripeBillingService.IsConfigured;

        // Pass order to view for context
        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> SavePaymentMethod(int id, string? acceptTerms)
    {
        var order = await GetOrderAsync(id);
        if (order == null) return NotFound();

        // Check if user owns this order
        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        var acceptedTerms = false;
        if (Request.HasFormContentType)
        {
            var submittedValues = Request.Form["acceptTerms"];
            acceptedTerms = submittedValues.Any(v =>
                string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(v, "on", StringComparison.OrdinalIgnoreCase));
        }

        if (!acceptedTerms)
            ModelState.AddModelError("", "You must accept the terms and conditions");

        if (!ModelState.IsValid)
        {
            ViewBag.StripeConfigured = _stripeBillingService.IsConfigured;
            return View(order);
        }
        

        try
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return Unauthorized();
            }

            if (!_stripeBillingService.IsConfigured)
            {
                ModelState.AddModelError("", "Stripe is not configured. Please contact support.");
                ViewBag.StripeConfigured = _stripeBillingService.IsConfigured;
                return View(order);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError("", "Unable to find your user profile.");
                ViewBag.StripeConfigured = _stripeBillingService.IsConfigured;
                return View(order);
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var successUrl = $"{baseUrl}/Orders/SavePaymentMethodSuccess/{id}?session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{baseUrl}/Orders/SavePaymentMethod/{id}";

            HttpContext.Session.SetString($"{SavePaymentTermsSessionPrefix}{id}", "true");

            var checkoutUrl = await _stripeBillingService.CreateSetupCheckoutSessionAsync(user, successUrl, cancelUrl, id);
            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error saving payment method: {ex.Message}");
            ViewBag.StripeConfigured = _stripeBillingService.IsConfigured;
            return View(order);
        }
    }

    [HttpGet]
    public async Task<IActionResult> SavePaymentMethodSuccess(int id, string? session_id)
    {
        var order = await GetOrderAsync(id);
        if (order == null) return NotFound();

        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        var acceptedTerms = string.Equals(
            HttpContext.Session.GetString($"{SavePaymentTermsSessionPrefix}{id}"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!acceptedTerms)
        {
            TempData["Error"] = "Please accept terms before saving your payment method.";
            return RedirectToAction("SavePaymentMethod", new { id });
        }

        if (string.IsNullOrWhiteSpace(session_id))
        {
            TempData["Error"] = "Missing checkout session details from Stripe.";
            return RedirectToAction("SavePaymentMethod", new { id });
        }

        if (User?.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            TempData["Error"] = "Unable to find your user profile.";
            return RedirectToAction("SavePaymentMethod", new { id });
        }

        try
        {
            await _stripeBillingService.FinalizeSetupCheckoutSessionAsync(user, session_id);

            var statusUpdatedViaApi = await _layeredApiOrderClient.UpdateOrderStatusAsync(id, "PendingPickup");
            var paymentStatusUpdatedViaApi = await _layeredApiOrderClient.UpdatePaymentStatusAsync(id, "PaymentMethodOnFile");

            if (!(statusUpdatedViaApi && paymentStatusUpdatedViaApi))
            {
                var localOrder = _orderStore.Get(id);
                if (localOrder != null)
                {
                    localOrder.UserEmail = email;
                    localOrder.Status = "PendingPickup";
                    localOrder.PaymentStatus = "PaymentMethodOnFile";
                    localOrder.LastUpdatedAt = DateTime.Now;
                    _orderStore.Save();
                }

                if (_apiOnlyMode)
                {
                    TempData["Success"] = "Payment method saved. Order sync is delayed, but your card is on file.";
                }
            }

            order.TermsAccepted = true;
            order.TermsAcceptedAt = DateTime.Now.ToString("o");
            order.PaymentStatus = "PaymentMethodOnFile";
            order.LastUpdatedAt = DateTime.Now;
            _orderStore.Save();

            HttpContext.Session.Remove($"{SavePaymentTermsSessionPrefix}{id}");

            return RedirectToAction("Confirm", new { id = order.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error confirming payment method: {ex.Message}";
            return RedirectToAction("SavePaymentMethod", new { id });
        }
    }

    private string DetermineCardBrand(string cardNumber)
    {
        if (cardNumber.StartsWith("4"))
            return "Visa";
        if (cardNumber.StartsWith("5"))
            return "Mastercard";
        if (cardNumber.StartsWith("3"))
            return "Amex";
        if (cardNumber.StartsWith("6"))
            return "Discover";
        return "Unknown";
    }

    [HttpGet]
    public async Task<IActionResult> Confirm(int id)
    {
        var order = await GetOrderAsync(id);
        if (order == null) return NotFound();

        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Payment(int id)
    {
        var order = await GetOrderAsync(id);
        if (order == null) return NotFound();
        
        // Check if user owns this order
        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> ProcessPayment(int id, string cardNumber, string expiry, string cvv, string cardholderName)
    {
        var order = await GetOrderAsync(id);
        if (order == null) return NotFound();
        
        // Check if user owns this order
        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        // Basic validation
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 13)
            ModelState.AddModelError("", "Invalid card number");
        
        if (string.IsNullOrWhiteSpace(expiry) || expiry.Length != 5)
            ModelState.AddModelError("", "Invalid expiry date (MM/YY)");
            
        if (string.IsNullOrWhiteSpace(cvv) || cvv.Length < 3)
            ModelState.AddModelError("", "Invalid CVV");
            
        if (string.IsNullOrWhiteSpace(cardholderName))
            ModelState.AddModelError("", "Cardholder name required");

        if (!ModelState.IsValid)
            return View("Payment", order);

        var attemptResult = await _layeredApiOrderClient.AttemptPaymentAsync(id);
        if (attemptResult.success)
        {
            if (attemptResult.status == "success")
            {
                var apiOrder = await _layeredApiOrderClient.GetOrderAsync(id) ?? order;
                var apiInvoice = await _layeredApiOrderClient.GetInvoiceAsync(id);
                var receiptAttempt = new PaymentAttempt
                {
                    OrderId = id,
                    Status = attemptResult.status,
                    Amount = attemptResult.amount,
                    TransactionId = attemptResult.transactionId,
                    AttemptNumber = 1,
                    CreatedAt = DateTime.Now
                };
                _ = _layeredApiJobClient.QueueReceiptEmailAsync(apiOrder, apiInvoice, receiptAttempt);
            }
        }
        else
        {
            if (_apiOnlyMode)
            {
                ModelState.AddModelError("", "Payment service is temporarily unavailable.");
                return View("Payment", order);
            }

            System.Threading.Thread.Sleep(2000);
            order.Status = "Paid";
            order.PaymentStatus = "Paid";
            _orderStore.Save();

            var invoice = _context.Invoices.FirstOrDefault(i => i.OrderId == id);
            var latestAttempt = _context.PaymentAttempts
                .Where(pa => pa.OrderId == id)
                .OrderByDescending(pa => pa.CreatedAt)
                .FirstOrDefault();

            _ = _layeredApiJobClient.QueueReceiptEmailAsync(order, invoice, latestAttempt);
        }

        return RedirectToAction("Receipt", new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(int id)
    {
        var order = await GetOrderAsync(id);
        if (order == null) return NotFound();

        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Invoice(int id)
    {
        var order = await GetOrderAsync(id);
        if (order == null) return NotFound();

        // Check if user owns this order
        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        var invoiceFromApi = await _layeredApiOrderClient.GetInvoiceAsync(id);
        if (invoiceFromApi != null)
        {
            order.Invoice = invoiceFromApi;
            order.InvoiceId = invoiceFromApi.Id;
        }
        else if (order.InvoiceId.HasValue)
        {
            var invoice = await _context.Invoices.FindAsync(order.InvoiceId.Value);
            if (invoice != null)
            {
                order.Invoice = invoice;
            }
        }

        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> UpdatePaymentMethod(int id)
    {
        var order = await GetOrderAsync(id);
        if (order == null) return NotFound();

        // Check if user owns this order
        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        ViewBag.StripeConfigured = _stripeBillingService.IsConfigured;
        return View(order);
    }

    [HttpPost]
    [ActionName("UpdatePaymentMethod")]
    public async Task<IActionResult> UpdatePaymentMethodPost(int id)
    {
        var order = await GetOrderAsync(id);
        if (order == null) return NotFound();

        // Check if user owns this order
        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        try
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return Unauthorized();
            }

            if (!_stripeBillingService.IsConfigured)
            {
                ModelState.AddModelError("", "Stripe is not configured. Please contact support.");
                ViewBag.StripeConfigured = _stripeBillingService.IsConfigured;
                return View(order);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError("", "Unable to find your user profile.");
                ViewBag.StripeConfigured = _stripeBillingService.IsConfigured;
                return View(order);
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var successUrl = $"{baseUrl}/Orders/UpdatePaymentMethodSuccess/{id}?session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{baseUrl}/Orders/UpdatePaymentMethod/{id}";
            var checkoutUrl = await _stripeBillingService.CreateSetupCheckoutSessionAsync(user, successUrl, cancelUrl, id);
            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error updating payment method: {ex.Message}");
            ViewBag.StripeConfigured = _stripeBillingService.IsConfigured;
            return View(order);
        }
    }

    [HttpGet]
    public async Task<IActionResult> UpdatePaymentMethodSuccess(int id, string? session_id)
    {
        var order = await GetOrderAsync(id);
        if (order == null) return NotFound();

        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        if (string.IsNullOrWhiteSpace(session_id))
        {
            TempData["Error"] = "Missing checkout session details from Stripe.";
            return RedirectToAction("UpdatePaymentMethod", new { id });
        }

        if (User?.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            TempData["Error"] = "Unable to find your user profile.";
            return RedirectToAction("UpdatePaymentMethod", new { id });
        }

        try
        {
            await _stripeBillingService.FinalizeSetupCheckoutSessionAsync(user, session_id);
            TempData["Success"] = "Payment method updated successfully.";
            return RedirectToAction("History");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error confirming payment method: {ex.Message}";
            return RedirectToAction("UpdatePaymentMethod", new { id });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RetryPayment(int id)
    {
        var order = await GetOrderAsync(id);
        if (order == null) return NotFound();

        // Check if user owns this order
        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        try
        {
            var retryApiResult = await _layeredApiOrderClient.RetryPaymentAsync(id);
            if (retryApiResult.success)
            {
                TempData["Success"] = retryApiResult.status == "success"
                    ? "Payment succeeded."
                    : "Payment retry initiated. Please check back shortly.";
            }
            else
            {
                await _paymentService.RetryPaymentAsync(id);
                TempData["Success"] = "Payment retry initiated. Please check back shortly.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error retrying payment: {ex.Message}";
        }

        return RedirectToAction("Invoice", new { id = id });
    }

    [HttpPost]
    public async Task<IActionResult> ApproveQuote(int id)
    {
        var email = User?.Identity?.Name ?? "";
        var order = await GetOrderAsync(id);
        if (order == null) return NotFound();
        if (order.UserEmail != email) return Forbid();

        var updatedViaApi = await _layeredApiOrderClient.UpdateOrderStatusAsync(id, "Approved");
        if (!updatedViaApi)
        {
            if (_apiOnlyMode)
            {
                TempData["Error"] = "Unable to approve quote right now.";
                return RedirectToAction("History");
            }

            order.Status = "Approved";
            order.PaymentStatus = "Approved";
            order.LastUpdatedAt = DateTime.Now;
            _orderStore.Save();
        }

        TempData["Success"] = "Final total approved. We’ll begin processing your order.";
        return RedirectToAction("History");
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var email = User?.Identity?.Name ?? "";
        var apiOrders = await _layeredApiOrderClient.GetUserOrdersAsync(email);

        List<LaundryOrder> orders;
        
        // Try API first if it returned data
        if (apiOrders != null && apiOrders.Count > 0)
        {
            orders = apiOrders;
        }
        // If API is unavailable or returned empty, fall back to local store for read visibility
        else
        {
            orders = _orderStore.ByUser(email).ToList();

            if (_apiOnlyMode && orders.Count == 0)
            {
                TempData["Error"] = "Order service is temporarily unavailable.";
            }
        }

        return View(orders);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var email = User?.Identity?.Name ?? "";
        var order = await GetOrderAsync(id);
        if (order == null) return NotFound();
        if (order.UserEmail != email) return Forbid();
        
        // Only allow cancellation of unpaid orders
        if (order.Status != "PendingPickup")
        {
            TempData["Error"] = "Cannot cancel orders that have already been paid.";
            return RedirectToAction("History");
        }

        var updatedViaApi = await _layeredApiOrderClient.UpdateOrderStatusAsync(id, "Cancelled");
        if (!updatedViaApi)
        {
            if (_apiOnlyMode)
            {
                TempData["Error"] = "Unable to cancel order right now.";
                return RedirectToAction("History");
            }

            order.Status = "Cancelled";
            _orderStore.Save();
        }

        return RedirectToAction("History");
    }

        private async Task<LaundryOrder?> GetOrderAsync(int id)
        {
            var orderFromApi = await _layeredApiOrderClient.GetOrderAsync(id);
            if (orderFromApi != null)
            {
                return orderFromApi;
            }

            return _orderStore.Get(id);
        }
}


