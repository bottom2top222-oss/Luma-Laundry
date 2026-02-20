using Microsoft.EntityFrameworkCore;
using LaundryApp.Data;
using LaundryApp.Middleware;
using Microsoft.AspNetCore.Identity;
using System.IO;
using LaundryApp.Models;
using LaundryApp.Services;
using Stripe;

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
builder.Services.AddScoped<StripeBillingService>();

var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrWhiteSpace(stripeSecretKey))
{
    StripeConfiguration.ApiKey = stripeSecretKey;
}

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
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
    .AddEntityFrameworkStores<LaundryAppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

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

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var isAllowedPath = path.StartsWith("/Account/ChangePassword", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Account/Logout", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase);

        if (!isAllowedPath)
        {
            var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
            var defaultAdminPassword = configuration["DefaultAdmin:Password"] ?? "Admin123!";

            var currentUser = await userManager.GetUserAsync(context.User);
            if (currentUser != null && await userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                var isUsingDefaultPassword = await userManager.CheckPasswordAsync(currentUser, defaultAdminPassword);
                if (isUsingDefaultPassword)
                {
                    var returnUrl = context.Request.Path + context.Request.QueryString;
                    var redirectUrl = $"/Account/ChangePassword?returnUrl={Uri.EscapeDataString(returnUrl)}";
                    context.Response.Redirect(redirectUrl);
                    return;
                }
            }
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SPA fallback for React app - serve index.html for non-MVC routes
app.MapFallbackToFile("index.html");

app.Run();




