namespace LaundryApp.Models;

public class PaymentMethod
{
    public int Id { get; set; }

    public string UserEmail { get; set; } = "";
    
    // Tokenized card info (not storing full card numbers)
    public string CardToken { get; set; } = "";
    public string CardLast4 { get; set; } = "";
    public string CardBrand { get; set; } = ""; // Visa, Mastercard, Amex, etc.
    public string ExpiryMonth { get; set; } = "";
    public string ExpiryYear { get; set; } = "";
    
    public bool IsDefault { get; set; } = false;
    public bool IsVerified { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastUsedAt { get; set; }
    
    public string GetDisplayName() => $"{CardBrand} ending in {CardLast4}";
}
