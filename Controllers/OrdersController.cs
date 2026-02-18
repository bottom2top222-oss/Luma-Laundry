using LaundryApp.Data;
using LaundryApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LaundryApp.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly OrderStore _orderStore;

    public OrdersController(OrderStore orderStore)
    {
        _orderStore = orderStore;
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

        return RedirectToAction("Payment", new { id = order.Id });
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
    public IActionResult Receipt(int id)
    {
        var order = _orderStore.Get(id);
        if (order == null) return NotFound();
        
        // Check if user owns this order
        var email = User?.Identity?.Name ?? "";
        if (order.UserEmail != email) return Forbid();

        return View(order);
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


