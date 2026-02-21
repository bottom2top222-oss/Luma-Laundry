using System.Text.Json;

namespace LaundryApp.Services;

public class QuoteCalculator
{
    private const decimal PersonalWashRate = 2.00m;
    private const decimal RequestWashRate = 2.25m;
    private const decimal WashMinimumLbs = 20m;
    private const decimal LargeBeddingMinimum = 50m;

    private static readonly Dictionary<string, (string Description, decimal UnitPrice, bool CountsTowardLargeMinimum)> Catalog =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["comforter_king"] = ("Comforter (King)", 34.99m, true),
            ["comforter_queen"] = ("Comforter (Queen)", 34.99m, true),
            ["comforter_full"] = ("Comforter (Full)", 32.99m, true),
            ["comforter_twin"] = ("Comforter (Twin)", 32.99m, true),
            ["duvet_cover"] = ("Duvet Cover", 19.99m, true),
            ["blanket"] = ("Blanket", 17.99m, true),
            ["bedspread"] = ("Bedspread", 15.99m, false),
            ["cushion_slip_cover"] = ("Cushion Slip Cover", 8.99m, false),
            ["chair_slip_cover"] = ("Chair Slip Cover", 17.99m, false),
            ["sofa_slip_cover"] = ("Sofa Slip Cover", 22.99m, false),
            ["pillow_sham"] = ("Pillow Sham", 3.99m, false),
            ["standard_pillow"] = ("Standard Pillow", 9.99m, false),
            ["mattress_cover"] = ("Mattress Cover", 11.99m, false)
        };

    private const string WeightedBlanketCode = "weighted_blanket";
    private const decimal WeightedBlanketRate = 2.85m;

    public QuoteCalculationResult Calculate(QuoteCalculationInput input)
    {
        var lineItems = new List<QuoteLineItem>();
        decimal subtotal = 0m;

        var washRate = string.Equals(input.PricingType, "Request", StringComparison.OrdinalIgnoreCase)
            ? RequestWashRate
            : PersonalWashRate;

        var washWeight = Math.Max(input.WashFoldWeightLbs.GetValueOrDefault(), 0m);
        if (washWeight > 0)
        {
            var billableWashWeight = Math.Max(washWeight, WashMinimumLbs);
            var washAmount = billableWashWeight * washRate;
            subtotal += washAmount;

            lineItems.Add(new QuoteLineItem(
                $"Wash & Fold ({billableWashWeight:0.##} lbs @ ${washRate:0.00}/lb)",
                washAmount));

            if (washWeight < WashMinimumLbs)
            {
                lineItems.Add(new QuoteLineItem("20 lb minimum applied", 0m));
            }
        }

        var hasLargeBeddingOrWeighted = false;

        foreach (var item in input.Items)
        {
            var itemCode = item.ItemCode?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(itemCode))
            {
                continue;
            }

            if (itemCode.Equals(WeightedBlanketCode, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!Catalog.TryGetValue(itemCode, out var catalogItem))
            {
                continue;
            }

            var quantity = Math.Max(item.Quantity, 0);
            if (quantity <= 0)
            {
                continue;
            }

            var amount = quantity * catalogItem.UnitPrice;
            subtotal += amount;

            lineItems.Add(new QuoteLineItem($"{catalogItem.Description} x{quantity}", amount));

            if (catalogItem.CountsTowardLargeMinimum)
            {
                hasLargeBeddingOrWeighted = true;
            }
        }

        var weightedBlanketWeight = Math.Max(input.WeightedBlanketWeightLbs.GetValueOrDefault(), 0m);
        weightedBlanketWeight += input.Items
            .Where(i => (i.ItemCode ?? string.Empty).Equals(WeightedBlanketCode, StringComparison.OrdinalIgnoreCase))
            .Sum(i => Math.Max(i.WeightLbs.GetValueOrDefault(), 0m));

        if (weightedBlanketWeight > 0)
        {
            var weightedAmount = weightedBlanketWeight * WeightedBlanketRate;
            subtotal += weightedAmount;
            hasLargeBeddingOrWeighted = true;
            lineItems.Add(new QuoteLineItem(
                $"Weighted Blanket ({weightedBlanketWeight:0.##} lbs @ ${WeightedBlanketRate:0.00}/lb)",
                weightedAmount));
        }

        var washMinimum = washWeight > 0 ? WashMinimumLbs * washRate : 0m;
        var largeMinimum = hasLargeBeddingOrWeighted ? LargeBeddingMinimum : 0m;
        var appliedMinimum = Math.Max(washMinimum, largeMinimum);

        if (appliedMinimum > 0 && subtotal < appliedMinimum)
        {
            lineItems.Add(new QuoteLineItem("Minimum pricing adjustment", appliedMinimum - subtotal));
            subtotal = appliedMinimum;
        }

        var estimatedTotal = ResolveEstimatedTotal(input);
        var requiresApproval = subtotal > appliedMinimum ||
            (estimatedTotal > 0m && subtotal > (estimatedTotal * 1.20m));

        var totalCents = ToCents(subtotal);
        var appliedMinimumCents = ToCents(appliedMinimum);

        return new QuoteCalculationResult(
            subtotal,
            totalCents,
            appliedMinimum,
            appliedMinimumCents,
            requiresApproval,
            JsonSerializer.Serialize(lineItems));
    }

    private static int ToCents(decimal amount)
    {
        return (int)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
    }

    private static decimal ResolveEstimatedTotal(QuoteCalculationInput input)
    {
        if (input.EstimatedTotalDollars.HasValue && input.EstimatedTotalDollars.Value > 0)
        {
            return input.EstimatedTotalDollars.Value;
        }

        if (input.EstimatedAmountCents.HasValue && input.EstimatedAmountCents.Value > 0)
        {
            return input.EstimatedAmountCents.Value / 100m;
        }

        return 0m;
    }
}

public sealed record QuoteCalculationInput(
    string PricingType,
    decimal? WashFoldWeightLbs,
    decimal? WeightedBlanketWeightLbs,
    IEnumerable<QuoteCalculationItemInput> Items,
    decimal? EstimatedTotalDollars,
    int? EstimatedAmountCents);

public sealed record QuoteCalculationItemInput(
    string ItemCode,
    int Quantity,
    decimal? WeightLbs = null);

public sealed record QuoteLineItem(string description, decimal amount);

public sealed record QuoteCalculationResult(
    decimal Total,
    int TotalCents,
    decimal AppliedMinimum,
    int AppliedMinimumCents,
    bool RequiresApproval,
    string LineItemsJson);