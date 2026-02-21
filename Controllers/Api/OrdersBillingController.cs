using System.Text.Json;
using LaundryApp.Data;
using LaundryApp.Models;
using LaundryApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace LaundryApp.Controllers.Api;

[ApiController]
[Route("api/orders")]
public class OrdersBillingController : ControllerBase
{
    private readonly LaundryAppDbContext _db;
    private readonly StripeBillingService _stripeBillingService;
    private readonly QuoteCalculator _quoteCalculator;

    public OrdersBillingController(LaundryAppDbContext db, StripeBillingService stripeBillingService, QuoteCalculator quoteCalculator)
    {
        _db = db;
        _stripeBillingService = stripeBillingService;
        _quoteCalculator = quoteCalculator;
    }

    [HttpPost("{orderId:int}/quote")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateQuote(int orderId, [FromBody] QuoteRequest request)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return NotFound();

        var quote = _quoteCalculator.Calculate(new QuoteCalculationInput(
            request.PricingType,
            request.BagWeightLbs,
            null,
            (request.Items ?? new List<QuoteItemInput>()).Select(i =>
                new QuoteCalculationItemInput(i.ItemCode, i.Quantity, i.WeightLbs)),
            null,
            request.EstimatedAmountCents));

        var requiresApproval = quote.RequiresApproval;

        order.PricingType = string.IsNullOrWhiteSpace(request.PricingType) ? "Personal" : request.PricingType;
        order.BagWeightLbs = request.BagWeightLbs;
        order.ItemsJson = JsonSerializer.Serialize(request.Items ?? new List<QuoteItemInput>());
        order.QuoteAmountCents = quote.TotalCents;
        order.FinalAmountCents = quote.TotalCents;
        order.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "usd" : request.Currency.ToLowerInvariant();
        order.Status = requiresApproval ? "Quoted" : "Approved";
        order.PaymentStatus = requiresApproval ? "ApprovalRequired" : "Approved";
        order.LastUpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            orderId,
            quoteAmountCents = order.QuoteAmountCents,
            finalAmountCents = order.FinalAmountCents,
            requiresApproval,
            status = order.Status
        });
    }

    [HttpPost("{orderId:int}/charge")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChargeOrder(int orderId)
    {
        try
        {
            var paymentIntent = await _stripeBillingService.ChargeOrderAsync(orderId);
            return Ok(new { paymentIntentId = paymentIntent.Id, status = paymentIntent.Status });
        }
        catch (StripeException ex)
        {
            return StatusCode(402, new
            {
                message = "Payment failed or requires customer action.",
                stripeCode = ex.StripeError?.Code,
                declineCode = ex.StripeError?.DeclineCode
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public class QuoteRequest
    {
        public string PricingType { get; set; } = "Personal";
        public decimal? BagWeightLbs { get; set; }
        public List<QuoteItemInput>? Items { get; set; }
        public int? EstimatedAmountCents { get; set; }
        public string Currency { get; set; } = "usd";
    }

    public class QuoteItemInput
    {
        public string ItemCode { get; set; } = "";
        public int Quantity { get; set; }
        public decimal? WeightLbs { get; set; }
    }
}
