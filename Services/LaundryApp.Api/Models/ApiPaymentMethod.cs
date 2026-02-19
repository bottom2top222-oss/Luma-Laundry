namespace LaundryApp.Api.Models;

public class ApiPaymentMethod
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = "";
    public string CardToken { get; set; } = "";
    public string CardLast4 { get; set; } = "";
    public string CardBrand { get; set; } = "";
    public string ExpiryMonth { get; set; } = "";
    public string ExpiryYear { get; set; } = "";
    public bool IsDefault { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastUsedAt { get; set; } = DateTime.Now;
}
