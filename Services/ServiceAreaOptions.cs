namespace LaundryApp.Services;

public class ServiceAreaOptions
{
    public bool Enabled { get; set; } = true;
    public string AllowedState { get; set; } = "FL";
    public List<string> AllowedCities { get; set; } = new();
    public List<string> AllowedZipCodes { get; set; } = new();
    public string OutOfAreaMessage { get; set; } = "We currently serve Brandon and Riverview, FL only.";
}
