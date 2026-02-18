using Microsoft.AspNetCore.Identity;

namespace LaundryApp.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string AddressLine1 { get; set; } = "";
    public string AddressLine2 { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
}