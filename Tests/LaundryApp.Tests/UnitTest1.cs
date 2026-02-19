using LaundryApp.Models;

namespace LaundryApp.Tests;

public class ModelBehaviorTests
{
    [Fact]
    public void PaymentMethod_GetDisplayName_UsesBrandAndLast4()
    {
        var method = new PaymentMethod
        {
            CardBrand = "Visa",
            CardLast4 = "4242"
        };

        var displayName = method.GetDisplayName();

        Assert.Equal("Visa ending in 4242", displayName);
    }

    [Fact]
    public void LaundryOrder_GetDisplayAddress_ComposesStructuredAddress()
    {
        var order = new LaundryOrder
        {
            AddressLine1 = "123 Main St",
            AddressLine2 = "Apt 4B",
            City = "Miami",
            State = "FL",
            ZipCode = "33101"
        };

        var address = order.GetDisplayAddress();

        Assert.Equal("123 Main St, Apt 4B, Miami, FL 33101", address);
    }

    [Fact]
    public void LaundryOrder_GetStatusBadge_ReturnsFallbackForUnknownStatus()
    {
        var order = new LaundryOrder
        {
            Status = "NotARealStatus"
        };

        var badge = order.GetStatusBadge();

        Assert.Equal("badge-secondary", badge);
    }
}
