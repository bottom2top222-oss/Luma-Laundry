using Microsoft.AspNetCore.Identity;
using LaundryApp.Models;

namespace LaundryApp.Data;

public static class IdentitySeed
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var config = sp.GetRequiredService<IConfiguration>();

        var roles = new[] { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminEmail = config["DefaultAdmin:Email"] ?? "admin@laundryapp.com";
        var adminPassword = config["DefaultAdmin:Password"] ?? "Admin123!";

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                PhoneNumber = "",
                AddressLine1 = "",
                AddressLine2 = "",
                City = "",
                State = "",
                ZipCode = ""
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
