namespace LaundryApp.Models;

public class PaymentAttempt
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public LaundryOrder? Order { get; set; }

    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    // Payment Status: success, failed, pending, refunded
    public string Status { get; set; } = "pending";

    // Attempt Details
    public decimal Amount { get; set; }
    public string FailureReason { get; set; } = "";
    public string TransactionId { get; set; } = "";
    
    // Retry Logic
    public int AttemptNumber { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? NextRetryAt { get; set; }

    public string GetStatusColor() => Status switch
    {
        "success" => "text-green-600",
        "failed" => "text-red-600",
        "pending" => "text-yellow-600",
        "refunded" => "text-blue-600",
        _ => "text-gray-600"
    };
}
