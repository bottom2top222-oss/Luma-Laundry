namespace LaundryApp.Worker;

public class QueuedEmailJob
{
    public Guid JobId { get; set; }
    public string JobType { get; set; } = "";
    public string ToEmail { get; set; } = "";
    public int OrderId { get; set; }
    public string ServiceType { get; set; } = "";
    public string ScheduledAt { get; set; } = "";
    public string Address { get; set; } = "";
    public decimal? Amount { get; set; }
    public string TransactionId { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}
