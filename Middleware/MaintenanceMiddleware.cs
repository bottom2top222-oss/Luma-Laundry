namespace LaundryApp.Middleware;

public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public MaintenanceMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if maintenance mode is enabled via environment variable
        string? maintenanceMode = Environment.GetEnvironmentVariable("MAINTENANCE_MODE");
        
        if (maintenanceMode == "true")
        {
            context.Response.StatusCode = 503;
            context.Response.ContentType = "text/html";
            
            var maintenanceHtml = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Maintenance - LUMA Laundry</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            background: linear-gradient(135deg, #0ea5e9 0%, #06b6d4 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }
        .container {
            text-align: center;
            background: white;
            border-radius: 16px;
            padding: 60px 40px;
            max-width: 500px;
            box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
        }
        .logo {
            font-size: 48px;
            margin-bottom: 20px;
        }
        h1 {
            color: #1e293b;
            font-size: 32px;
            margin-bottom: 15px;
            font-weight: 700;
        }
        p {
            color: #64748b;
            font-size: 16px;
            line-height: 1.6;
            margin-bottom: 30px;
        }
        .status {
            display: inline-block;
            background: #fef3c7;
            color: #92400e;
            padding: 10px 16px;
            border-radius: 8px;
            font-size: 14px;
            font-weight: 600;
            margin-top: 20px;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""logo"">🧺</div>
        <h1>LUMA Laundry</h1>
        <h2 style=""color: #64748b; font-size: 22px; font-weight: 600; margin-bottom: 20px;"">Under Maintenance</h2>
        <p>We're currently updating our service to serve you better. We'll be back online shortly.</p>
        <p style=""color: #94a3b8; font-size: 14px;"">Thank you for your patience!</p>
        <div class=""status"">HTTP 503 - Service Unavailable</div>
    </div>
</body>
</html>
";
            await context.Response.WriteAsync(maintenanceHtml);
            return;
        }

        await _next(context);
    }
}
