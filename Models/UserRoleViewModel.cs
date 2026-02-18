namespace LaundryApp.Models;

public class UserRoleViewModel
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string UserName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string AddressLine1 { get; set; } = "";
    public string AddressLine2 { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public bool IsAdmin { get; set; }
    public string Roles { get; set; } = "";
}