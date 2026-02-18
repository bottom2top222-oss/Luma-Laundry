namespace LaundryApp.Models;

public class AuditLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string UserEmail { get; set; } = "";
    public string Action { get; set; } = "";
    public string Entity { get; set; } = "";
    public string Details { get; set; } = "";
}