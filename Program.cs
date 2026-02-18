using Microsoft.EntityFrameworkCore;
using LaundryApp.Data;
using Microsoft.AspNetCore.Identity;
using System.IO;
using LaundryApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

// Database
var dbPath = builder.Environment.IsDevelopment() 
    ? Path.Combine(Directory.GetCurrentDirectory(), "laundry.db")
    : Path.Combine("/var", "data", "laundry.db");  // Production path

builder.Services.AddDbContext<LaundryApp.Data.LaundryAppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// OrderStore
builder.Services.AddScoped<LaundryApp.Data.OrderStore>();

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
    .AddEntityFrameworkStores<LaundryAppDbContext>()
    .AddDefaultTokenProviders();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LaundryApp.Data.LaundryAppDbContext>();
    dbContext.Database.Migrate();
}

// Seed default roles and admin user (synchronously)
IdentitySeed.SeedAsync(app.Services).GetAwaiter().GetResult();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SPA fallback for React app - serve index.html for all /app routes
app.MapFallbackToFile("/app/{**slug}", "/app/index.html");

app.Run();




