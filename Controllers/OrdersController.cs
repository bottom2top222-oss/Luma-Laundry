using LaundryApp.Data;
using LaundryApp.Models;
using LaundryApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LaundryApp.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly OrderStore _orderStore;
    private readonly LaundryAppDbContext _context;
    private readonly PaymentService _paymentService;

    public OrdersController(OrderStore orderStore, LaundryAppDbContext context, PaymentService paymentService)
    {
        _orderStore = orderStore;
        _context = context;
        _paymentService = paymentService;
    }

    [HttpGet]
    public IActionResult Schedule()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Schedule(string serviceType, DateTime scheduledAt, string addressLine1, string? addressLine2, string city, string state, string zipCode, string? notes)
    {
        if (string.IsNullOrWhiteSpace(serviceType))
            ModelState.AddModelError("", "Choose Pickup, Drop-off, or Both.");

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
            Status = "Scheduled",
            UserEmail = User?.Identity?.Name ?? ""
        };

        order.Address = order.GetDisplayAddress();

        _orderStore.Add(order);

        // Redirect to payment method setup instead of payment
        return RedirectToAction("SavePaymentMethod", new { id = order.Id });
    }

    [HttpGet]
    public IActionResult SavePaymentMethod(int id)
    {
        var order = _orderStore.Get(id);
        if (order == null) return NotFound();

        // Check if user owns this order
        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        // Pass order to view for context
        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> SavePaymentMethod(int id, string cardNumber, string expiry, string cvv, string cardholderName, bool acceptTerms)
    {
        var order = _orderStore.Get(id);
        if (order == null) return NotFound();

        // Check if user owns this order
        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        // Validate card details
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 13)
            ModelState.AddModelError("", "Invalid card number");

        if (string.IsNullOrWhiteSpace(expiry) || expiry.Length != 5)
            ModelState.AddModelError("", "Invalid expiry date (MM/YY)");

        if (string.IsNullOrWhiteSpace(cvv) || cvv.Length < 3)
            ModelState.AddModelError("", "Invalid CVV");

        if (string.IsNullOrWhiteSpace(cardholderName))
            ModelState.AddModelError("", "Cardholder name required");

        if (!acceptTerms)
            ModelState.AddModelError("", "You must accept the terms and conditions");

        if (!ModelState.IsValid)
            return View(order);

        try
        {
            // Extract card details for saving
            string cardLast4 = cardNumber.Substring(cardNumber.Length - 4);
            string cardBrand = DetermineCardBrand(cardNumber);
            string expiryMonth = expiry.Split('/')[0];
            string expiryYear = "20" + expiry.Split('/')[1];

            // Save payment method (tokenized card)
            await _paymentService.SavePaymentMethodAsync(
                email,
                cardNumber, // In real app, this would be tokenized by Stripe first
                cardLast4,
                cardBrand,
                expiryMonth,
                expiryYear,
                isDefault: true
            );

            // Update order with payment method saved and terms accepted
            order.TermsAccepted = true;
            order.TermsAcceptedAt = DateTime.Now.ToString("o");
            order.PaymentStatus = "method_on_file";
            _orderStore.Save();

            return RedirectToAction("Confirm", new { id = order.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error saving payment method: {ex.Message}");
            return View(order);
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
    public IActionResult Confirm(int id)
    {
        var order = _orderStore.Get(id);
        if (order == null) return NotFound();

        return View(order);
    }

    [HttpGet]
    public IActionResult Payment(int id)
    {
        var order = _orderStore.Get(id);
        if (order == null) return NotFound();
        
        // Check if user owns this order
        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        return View(order);
    }

    [HttpPost]
    public IActionResult ProcessPayment(int id, string cardNumber, string expiry, string cvv, string cardholderName)
    {
        var order = _orderStore.Get(id);
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

        // Mock payment processing
        // In real app, this would integrate with Stripe, PayPal, etc.
        System.Threading.Thread.Sleep(2000); // Simulate processing delay
        
        // Mark as paid and update status
        order.Status = "Paid";
        _orderStore.Save();

        return RedirectToAction("Receipt", new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Invoice(int id)
    {
        var order = _orderStore.Get(id);
        if (order == null) return NotFound();

        // Check if user owns this order
        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        // Load invoice details from database if exists
        if (order.InvoiceId.HasValue)
        {
            var invoice = await _context.Invoices.FindAsync(order.InvoiceId.Value);
            if (invoice != null)
            {
                // Load full order with invoice relationship
                var fullOrder = await _context.Orders
                    .Include(o => o.Invoice)
                    .FirstOrDefaultAsync(o => o.Id == id);
                if (fullOrder != null)
                    order = fullOrder;
            }
        }

        return View(order);
    }

    [HttpGet]
    public IActionResult UpdatePaymentMethod(int id)
    {
        var order = _orderStore.Get(id);
        if (order == null) return NotFound();

        // Check if user owns this order
        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePaymentMethod(int id, string cardNumber, string expiry, string cvv, string cardholderName)
    {
        var order = _orderStore.Get(id);
        if (order == null) return NotFound();

        // Check if user owns this order
        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        // Validate card details
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 13)
            ModelState.AddModelError("", "Invalid card number");

        if (string.IsNullOrWhiteSpace(expiry) || expiry.Length != 5)
            ModelState.AddModelError("", "Invalid expiry date (MM/YY)");

        if (string.IsNullOrWhiteSpace(cvv) || cvv.Length < 3)
            ModelState.AddModelError("", "Invalid CVV");

        if (string.IsNullOrWhiteSpace(cardholderName))
            ModelState.AddModelError("", "Cardholder name required");

        if (!ModelState.IsValid)
            return View(order);

        try
        {
            // Extract card details for saving
            string cardLast4 = cardNumber.Substring(cardNumber.Length - 4);
            string cardBrand = DetermineCardBrand(cardNumber);
            string expiryMonth = expiry.Split('/')[0];
            string expiryYear = "20" + expiry.Split('/')[1];

            // Save new payment method
            await _paymentService.SavePaymentMethodAsync(
                email,
                cardNumber, // In real app, this would be tokenized by Stripe first
                cardLast4,
                cardBrand,
                expiryMonth,
                expiryYear,
                isDefault: true
            );

            // Get the newly saved payment method and update order
            var paymentMethod = await _context.PaymentMethods
                .Where(pm => pm.UserEmail == email && pm.IsDefault)
                .FirstOrDefaultAsync();

            if (paymentMethod != null)
            {
                var dbOrder = await _context.Orders.FindAsync(id);
                if (dbOrder != null)
                {
                    dbOrder.PaymentMethodId = paymentMethod.Id;
                    dbOrder.PaymentStatus = "method_on_file";
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Payment method updated successfully.";
            return RedirectToAction("History");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error updating payment method: {ex.Message}");
            return View(order);
        }
    }

    [HttpPost]
    public async Task<IActionResult> RetryPayment(int id)
    {
        var order = _orderStore.Get(id);
        if (order == null) return NotFound();

        // Check if user owns this order
        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        try
        {
            // Retry payment for this order
            await _paymentService.RetryPaymentAsync(id);
            TempData["Success"] = "Payment retry initiated. Please check back shortly.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error retrying payment: {ex.Message}";
        }

        return RedirectToAction("Invoice", new { id = id });
    }

    [HttpGet]
    public IActionResult History()
    {
        var email = User?.Identity?.Name ?? "";
        var orders = _orderStore.All()
            .Where(o => o.UserEmail == email)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        return View(orders);
    }

    [HttpPost]
    public IActionResult Cancel(int id)
    {
        var email = User?.Identity?.Name ?? "";
        var order = _orderStore.Get(id);
        if (order == null) return NotFound();
        if (order.UserEmail != email) return Forbid();
        
        // Only allow cancellation of unpaid orders
        if (order.Status != "Scheduled")
        {
            TempData["Error"] = "Cannot cancel orders that have already been paid.";
            return RedirectToAction("History");
        }

        order.Status = "Cancelled";
        _orderStore.Save();
        return RedirectToAction("History");
    }
}


