namespace LaundryApp.Api.Models;

public class ApiInvoice
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Status { get; set; } = "draft";
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Tip { get; set; }
    public decimal Total { get; set; }
    public string LineItems { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime FinalizedAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime? VoidedAt { get; set; }
}
