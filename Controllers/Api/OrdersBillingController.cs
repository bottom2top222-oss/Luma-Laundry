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

    public OrdersBillingController(LaundryAppDbContext db, StripeBillingService stripeBillingService)
    {
        _db = db;
        _stripeBillingService = stripeBillingService;
    }

    [HttpPost("{orderId:int}/quote")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateQuote(int orderId, [FromBody] QuoteRequest request)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return NotFound();

        var quote = CalculateQuote(request);
        var requiresApproval = quote.requiresApproval;

        order.PricingType = string.IsNullOrWhiteSpace(request.PricingType) ? "Personal" : request.PricingType;
        order.BagWeightLbs = request.BagWeightLbs;
        order.ItemsJson = JsonSerializer.Serialize(request.Items ?? new List<QuoteItemInput>());
        order.QuoteAmountCents = quote.totalCents;
        order.FinalAmountCents = quote.totalCents;
        order.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "usd" : request.Currency.ToLowerInvariant();
        order.Status = requiresApproval ? "ApprovalRequired" : "Quoted";
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

    private static (int totalCents, bool requiresApproval) CalculateQuote(QuoteRequest request)
    {
        var pricingType = string.IsNullOrWhiteSpace(request.PricingType) ? "Personal" : request.PricingType;
        var washRate = pricingType.Equals("Request", StringComparison.OrdinalIgnoreCase) ? 2.25m : 2.00m;

        var washWeight = request.BagWeightLbs.GetValueOrDefault(0m);
        var washChargeableWeight = washWeight > 0 ? Math.Max(washWeight, 20m) : 0m;
        var washSubtotal = washChargeableWeight * washRate;

        var itemsSubtotal = 0m;
        var hasLargeBeddingOrWeighted = false;

        foreach (var item in request.Items ?? new List<QuoteItemInput>())
        {
            var qty = Math.Max(item.Quantity, 0);
            if (qty == 0 && item.ItemCode != "weighted_blanket") continue;

            var unitPrice = item.ItemCode switch
            {
                "comforter_king" => 34.99m,
                "comforter_queen" => 34.99m,
                "comforter_full" => 32.99m,
                "comforter_twin" => 32.99m,
                "duvet_cover" => 19.99m,
                "blanket" => 17.99m,
                "bedspread" => 15.99m,
                "cushion_slip_cover" => 8.99m,
                "chair_slip_cover" => 17.99m,
                "sofa_slip_cover" => 22.99m,
                "pillow_sham" => 3.99m,
                "standard_pillow" => 9.99m,
                "mattress_cover" => 11.99m,
                "weighted_blanket" => 2.85m,
                _ => 0m
            };

            if (unitPrice <= 0) continue;

            if (item.ItemCode == "weighted_blanket")
            {
                var wbWeight = item.WeightLbs.GetValueOrDefault(0m);
                if (wbWeight > 0)
                {
                    itemsSubtotal += wbWeight * unitPrice;
                    hasLargeBeddingOrWeighted = true;
                }
            }
            else
            {
                itemsSubtotal += qty * unitPrice;
                if (item.ItemCode.StartsWith("comforter") || item.ItemCode is "duvet_cover" or "blanket")
                {
                    hasLargeBeddingOrWeighted = true;
                }
            }
        }

        if (hasLargeBeddingOrWeighted && itemsSubtotal < 50m)
        {
            itemsSubtotal = 50m;
        }

        var total = washSubtotal + itemsSubtotal;
        var totalCents = (int)Math.Round(total * 100m, MidpointRounding.AwayFromZero);

        var minWashTotal = washWeight > 0 ? (int)Math.Round((20m * washRate) * 100m, MidpointRounding.AwayFromZero) : 0;
        var minLargeTotal = hasLargeBeddingOrWeighted ? 5000 : 0;
        var baselineMinimum = Math.Max(minWashTotal, minLargeTotal);

        var requiresApproval = totalCents > baselineMinimum;
        if (request.EstimatedAmountCents.HasValue && request.EstimatedAmountCents.Value > 0)
        {
            requiresApproval = requiresApproval || totalCents > (int)Math.Round(request.EstimatedAmountCents.Value * 1.20m, MidpointRounding.AwayFromZero);
        }

        return (Math.Max(totalCents, 0), requiresApproval);
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
