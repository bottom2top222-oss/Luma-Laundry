namespace LaundryApp.Models;

public class LaundryOrder
{
    public int Id { get; set; }

    public string UserEmail { get; set; } = "";
    public string ServiceType { get; set; } = "";
    public string PricingType { get; set; } = "Personal";
    public DateTime ScheduledAt { get; set; }

    public string Address { get; set; } = "";
    public string AddressLine1 { get; set; } = "";
    public string AddressLine2 { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string Notes { get; set; } = "";
    public string AdminNotes { get; set; } = "";

    // Order Status: PendingPickup, PickedUp, WeighedOrCounted, Quoted, Approved, InProgress, Ready, Paid, Delivered, Completed, Cancelled
    public string Status { get; set; } = "PendingPickup";
    
    // Payment Status: NoPaymentMethod, PaymentMethodOnFile, ApprovalRequired, Approved, ChargeAttempted, Paid, PaymentFailed, Refunded
    public string PaymentStatus { get; set; } = "NoPaymentMethod";
    
    // Payment Fields
    public int? PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    
    public bool TermsAccepted { get; set; } = false;
    public string TermsAcceptedAt { get; set; } = "";

    public decimal? BagWeightLbs { get; set; }
    public string ItemsJson { get; set; } = "[]";
    public int QuoteAmountCents { get; set; }
    public int FinalAmountCents { get; set; }
    public string Currency { get; set; } = "usd";
    public string StripePaymentIntentId { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastUpdatedAt { get; set; } = DateTime.Now;
    public DateTime? ClosedAt { get; set; }

    public string GetDisplayAddress()
    {
        if (string.IsNullOrWhiteSpace(AddressLine1)
            && string.IsNullOrWhiteSpace(AddressLine2)
            && string.IsNullOrWhiteSpace(City)
            && string.IsNullOrWhiteSpace(State)
            && string.IsNullOrWhiteSpace(ZipCode))
        {
            return Address;
        }

        var line1 = AddressLine1.Trim();
        if (!string.IsNullOrWhiteSpace(AddressLine2))
        {
            var line2Part = AddressLine2.Trim();
            line1 = string.IsNullOrWhiteSpace(line1) ? line2Part : $"{line1}, {line2Part}";
        }

        var line2 = City.Trim();
        if (!string.IsNullOrWhiteSpace(State))
        {
            var statePart = State.Trim();
            line2 = string.IsNullOrWhiteSpace(line2) ? statePart : $"{line2}, {statePart}";
        }

        if (!string.IsNullOrWhiteSpace(ZipCode))
        {
            var zipPart = ZipCode.Trim();
            line2 = string.IsNullOrWhiteSpace(line2) ? zipPart : $"{line2} {zipPart}";
        }

        if (string.IsNullOrWhiteSpace(line1))
        {
            return line2;
        }

        if (string.IsNullOrWhiteSpace(line2))
        {
            return line1;
        }

        return $"{line1}, {line2}";
    }
    
    public string GetStatusBadge() => Status switch
    {
        "PendingPickup" => "badge-secondary",
        "PickedUp" => "badge-info",
        "WeighedOrCounted" => "badge-primary",
        "Quoted" => "badge-warning",
        "Approved" => "badge-info",
        "InProgress" => "badge-primary",
        "Ready" => "badge-info",
        "PaymentFailed" => "badge-danger",
        "Delivered" => "badge-success",
        "Paid" => "badge-success",
        "Completed" => "badge-dark",
        "Cancelled" => "badge-danger",
        _ => "badge-secondary"
    };
}




