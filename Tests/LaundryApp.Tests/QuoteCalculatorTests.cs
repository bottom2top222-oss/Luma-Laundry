using LaundryApp.Services;

namespace LaundryApp.Tests;

public class QuoteCalculatorTests
{
    private readonly QuoteCalculator _calculator = new();

    [Fact]
    public void Calculate_AppliesTwentyPoundWashMinimum()
    {
        var result = _calculator.Calculate(new QuoteCalculationInput(
            PricingType: "Personal",
            WashFoldWeightLbs: 10m,
            WeightedBlanketWeightLbs: null,
            Items: [],
            EstimatedTotalDollars: null,
            EstimatedAmountCents: null));

        Assert.Equal(40.00m, result.Total);
        Assert.Equal(4000, result.TotalCents);
        Assert.Equal(40.00m, result.AppliedMinimum);
        Assert.DoesNotContain("Minimum pricing adjustment", result.LineItemsJson);
        Assert.False(result.RequiresApproval);
    }

    [Fact]
    public void Calculate_AppliesLargeBeddingMinimum()
    {
        var result = _calculator.Calculate(new QuoteCalculationInput(
            PricingType: "Personal",
            WashFoldWeightLbs: null,
            WeightedBlanketWeightLbs: null,
            Items:
            [
                new QuoteCalculationItemInput("blanket", 1)
            ],
            EstimatedTotalDollars: null,
            EstimatedAmountCents: null));

        Assert.Equal(50.00m, result.Total);
        Assert.Equal(5000, result.TotalCents);
        Assert.Equal(50.00m, result.AppliedMinimum);
        Assert.Contains("Minimum pricing adjustment", result.LineItemsJson);
        Assert.False(result.RequiresApproval);
    }

    [Fact]
    public void Calculate_HandlesWeightedBlanketByWeight()
    {
        var result = _calculator.Calculate(new QuoteCalculationInput(
            PricingType: "Personal",
            WashFoldWeightLbs: null,
            WeightedBlanketWeightLbs: null,
            Items:
            [
                new QuoteCalculationItemInput("weighted_blanket", 0, 25m)
            ],
            EstimatedTotalDollars: null,
            EstimatedAmountCents: null));

        Assert.Equal(71.25m, result.Total);
        Assert.Equal(7125, result.TotalCents);
        Assert.True(result.RequiresApproval);
    }

    [Fact]
    public void Calculate_RequiresApprovalWhenOverEstimatedThreshold()
    {
        var result = _calculator.Calculate(new QuoteCalculationInput(
            PricingType: "Personal",
            WashFoldWeightLbs: 20m,
            WeightedBlanketWeightLbs: null,
            Items: [],
            EstimatedTotalDollars: 30m,
            EstimatedAmountCents: null));

        Assert.Equal(40.00m, result.Total);
        Assert.True(result.RequiresApproval);
    }
}
