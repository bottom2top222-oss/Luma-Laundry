using Microsoft.EntityFrameworkCore;
using LaundryApp.Data;
using LaundryApp.Middleware;
using Microsoft.AspNetCore.Identity;
using System.IO;
using LaundryApp.Models;
using LaundryApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

// Database
var configuredDbPath = builder.Configuration["Database:Path"];
var dbPath = !string.IsNullOrWhiteSpace(configuredDbPath)
    ? configuredDbPath
    : builder.Environment.IsDevelopment()
        ? Path.Combine(Directory.GetCurrentDirectory(), "laundry.db")
        : OperatingSystem.IsWindows()
            ? Path.Combine(Directory.GetCurrentDirectory(), "laundry.db")
            : Path.Combine("/var", "data", "laundry.db");

var dbDirectory = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrWhiteSpace(dbDirectory))
{
    Directory.CreateDirectory(dbDirectory);
}

builder.Services.AddDbContext<LaundryApp.Data.LaundryAppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// OrderStore
builder.Services.AddScoped<LaundryApp.Data.OrderStore>();

// Payment Service
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<EmailNotificationService>();

var layeredApiBaseUrl = builder.Configuration["LayeredServices:ApiBaseUrl"] ?? "http://localhost:5080";
builder.Services.AddHttpClient<LayeredApiJobClient>(client =>
{
    client.BaseAddress = new Uri(layeredApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddHttpClient<LayeredApiOrderClient>(client =>
{
    client.BaseAddress = new Uri(layeredApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(8);
});

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

// Maintenance mode middleware
app.UseMiddleware<MaintenanceMiddleware>();

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SPA fallback for React app - serve index.html for non-MVC routes
app.MapFallbackToFile("index.html");

app.Run();




