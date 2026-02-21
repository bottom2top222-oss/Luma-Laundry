using Microsoft.EntityFrameworkCore;
using LaundryApp.Data;
using LaundryApp.Middleware;
using Microsoft.AspNetCore.Identity;
using System.IO;
using LaundryApp.Models;
using LaundryApp.Services;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

var stripeSecretFromEnv = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")?.Trim();
var stripePublishableFromEnv = Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE_KEY")?.Trim();
var stripeWebhookFromEnv = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")?.Trim();

var stripeOverrides = new Dictionary<string, string?>();
if (!string.IsNullOrWhiteSpace(stripeSecretFromEnv))
{
    stripeOverrides["Stripe:SecretKey"] = stripeSecretFromEnv;
}

if (!string.IsNullOrWhiteSpace(stripePublishableFromEnv))
{
    stripeOverrides["Stripe:PublishableKey"] = stripePublishableFromEnv;
}

if (!string.IsNullOrWhiteSpace(stripeWebhookFromEnv))
{
    stripeOverrides["Stripe:WebhookSecret"] = stripeWebhookFromEnv;
}

if (stripeOverrides.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(stripeOverrides);
}

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
builder.Services.AddScoped<QuoteCalculator>();

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

static bool HasSuspiciousStripeChars(string value)
{
    if (string.IsNullOrEmpty(value)) return false;

    return value.Any(char.IsWhiteSpace)
        || value.Contains(';')
        || value.Contains('"')
        || value.Contains('\'')
        || value.Contains("&#")
        || value.Contains("&quot;", StringComparison.OrdinalIgnoreCase)
        || value.Contains("&apos;", StringComparison.OrdinalIgnoreCase);
}

void WarnIfSuspiciousStripeValue(string keyName, string? value)
{
    if (string.IsNullOrWhiteSpace(value)) return;

    if (HasSuspiciousStripeChars(value))
    {
        app.Logger.LogWarning("{ConfigKey} appears malformed (contains whitespace or encoded/special characters). Re-enter the value in environment variables as a single plain line.", keyName);
    }
}

WarnIfSuspiciousStripeValue("Stripe:PublishableKey", app.Configuration["Stripe:PublishableKey"]?.Trim());
WarnIfSuspiciousStripeValue("Stripe:SecretKey", app.Configuration["Stripe:SecretKey"]?.Trim());
WarnIfSuspiciousStripeValue("Stripe:WebhookSecret", app.Configuration["Stripe:WebhookSecret"]?.Trim());

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




