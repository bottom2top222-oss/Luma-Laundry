namespace LaundryApp.Api.Models;

public class ApiLaundryOrder
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = "";
    public string ServiceType { get; set; } = "";
    public DateTime ScheduledAt { get; set; }

    public string Address { get; set; } = "";
    public string AddressLine1 { get; set; } = "";
    public string AddressLine2 { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string Notes { get; set; } = "";
    public string AdminNotes { get; set; } = "";

    public string Status { get; set; } = "Scheduled";
    public string PaymentStatus { get; set; } = "method_on_file";

    public int? PaymentMethodId { get; set; }
    public int? InvoiceId { get; set; }

    public bool TermsAccepted { get; set; }
    public string TermsAcceptedAt { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastUpdatedAt { get; set; } = DateTime.Now;
    public DateTime? ClosedAt { get; set; }
}
