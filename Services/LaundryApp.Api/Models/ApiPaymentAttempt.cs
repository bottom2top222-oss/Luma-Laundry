namespace LaundryApp.Api.Models;

public class ApiPaymentAttempt
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int? InvoiceId { get; set; }
    public string Status { get; set; } = "pending";
    public decimal Amount { get; set; }
    public string FailureReason { get; set; } = "";
    public string TransactionId { get; set; } = "";
    public int AttemptNumber { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? NextRetryAt { get; set; }
}
