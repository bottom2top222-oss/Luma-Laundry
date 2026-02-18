namespace LaundryApp.Models;

public class OrderWithUserViewModel
{
    public LaundryOrder Order { get; set; } = null!;
    public string UserName { get; set; } = "";
}