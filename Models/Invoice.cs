namespace LaundryApp.Models;

public class Invoice
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public LaundryOrder? Order { get; set; }

    // Invoice Status: draft, final, locked, void
    public string Status { get; set; } = "draft";

    // Invoice Details
    public decimal SubTotal { get; set; } = 0;
    public decimal TaxAmount { get; set; } = 0;
    public decimal DeliveryFee { get; set; } = 0;
    public decimal Tip { get; set; } = 0;
    public decimal Total { get; set; } = 0;

    // Line Items (stored as JSON or separate table)
    public string LineItems { get; set; } = "[]"; // JSON array of items: [{name, quantity, price}]

    // Timeline
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime FinalizedAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime? VoidedAt { get; set; }

    public string GetStatusBadge() => Status switch
    {
        "draft" => "bg-gray-500",
        "final" => "bg-blue-500",
        "locked" => "bg-green-500",
        "void" => "bg-red-500",
        _ => "bg-gray-500"
    };
}
