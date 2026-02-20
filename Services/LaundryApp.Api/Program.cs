using System.Collections.Concurrent;
using System.Globalization;
using LaundryApp.Api.Data;
using LaundryApp.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var configuredConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var fallbackDbPath = ApiHelpers.ResolveSharedDbPath();
var connectionString = string.IsNullOrWhiteSpace(configuredConnection)
    ? $"Data Source={fallbackDbPath}"
    : configuredConnection;

builder.Services.AddDbContext<ApiDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

var queue = new ConcurrentQueue<QueuedEmailJob>();

app.MapGet("/health", () => Results.Ok(new
{
    service = "LaundryApp.Api",
    status = "ok",
    queuedJobs = queue.Count,
    utc = DateTime.UtcNow
}));

app.MapPost("/api/orders", async (CreateOrderRequest request, ApiDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.UserEmail) || string.IsNullOrWhiteSpace(request.ServiceType))
    {
        return Results.BadRequest(new { error = "UserEmail and ServiceType are required." });
    }

    if (request.ScheduledAt == default)
    {
        return Results.BadRequest(new { error = "ScheduledAt is required." });
    }

    var normalizedUserEmail = request.UserEmail.Trim();

    var order = new ApiLaundryOrder
    {
        UserEmail = normalizedUserEmail,
        ServiceType = request.ServiceType,
        ScheduledAt = request.ScheduledAt,
        AddressLine1 = request.AddressLine1?.Trim() ?? "",
        AddressLine2 = request.AddressLine2?.Trim() ?? "",
        City = request.City?.Trim() ?? "",
        State = request.State?.Trim() ?? "",
        ZipCode = request.ZipCode?.Trim() ?? "",
        Notes = request.Notes?.Trim() ?? "",
        Address = ApiHelpers.BuildAddress(request.AddressLine1, request.AddressLine2, request.City, request.State, request.ZipCode),
        Status = "PendingPickup",
        PaymentStatus = "NoPaymentMethod",
        CreatedAt = DateTime.Now,
        LastUpdatedAt = DateTime.Now
    };

    db.Orders.Add(order);
    await db.SaveChangesAsync();

    return Results.Ok(new { orderId = order.Id });
});

app.MapGet("/api/orders/{id:int}", async (int id, ApiDbContext db) =>
{
    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

app.MapPost("/api/orders/{id:int}/invoice/generate", async (int id, GenerateInvoiceRequest request, ApiDbContext db) =>
{
    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
    if (order is null)
    {
        return Results.NotFound();
    }

    var existingInvoice = await db.Invoices.FirstOrDefaultAsync(i => i.OrderId == id);
    if (existingInvoice != null)
    {
        if (!order.InvoiceId.HasValue)
        {
            order.InvoiceId = existingInvoice.Id;
            order.LastUpdatedAt = DateTime.Now;
            await db.SaveChangesAsync();
        }

        return Results.Ok(new { invoiceId = existingInvoice.Id });
    }

    var total = request.Subtotal + request.TaxAmount + request.DeliveryFee;

    var invoice = new ApiInvoice
    {
        OrderId = id,
        Status = "draft",
        SubTotal = request.Subtotal,
        TaxAmount = request.TaxAmount,
        DeliveryFee = request.DeliveryFee,
        Tip = 0,
        Total = total,
        LineItems = string.IsNullOrWhiteSpace(request.LineItems) ? "[]" : request.LineItems,
        CreatedAt = DateTime.Now,
        FinalizedAt = DateTime.Now
    };

    db.Invoices.Add(invoice);
    await db.SaveChangesAsync();

    order.InvoiceId = invoice.Id;
    order.Status = "Quoted";
    order.PaymentStatus = "ApprovalRequired";
    order.LastUpdatedAt = DateTime.Now;
    await db.SaveChangesAsync();

    return Results.Ok(new { invoiceId = invoice.Id });
});

app.MapPost("/api/orders/{id:int}/payment/attempt", async (int id, ApiDbContext db) =>
{
    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
    if (order is null)
    {
        return Results.NotFound();
    }

    if (!order.PaymentMethodId.HasValue)
    {
        return Results.BadRequest(new { error = "Payment method not found." });
    }

    var invoice = order.InvoiceId.HasValue
        ? await db.Invoices.FirstOrDefaultAsync(i => i.Id == order.InvoiceId.Value)
        : await db.Invoices.FirstOrDefaultAsync(i => i.OrderId == id);

    if (invoice is null)
    {
        return Results.BadRequest(new { error = "Invoice not found." });
    }

    var lastAttempt = await db.PaymentAttempts
        .Where(pa => pa.OrderId == id)
        .OrderByDescending(pa => pa.CreatedAt)
        .FirstOrDefaultAsync();

    var attemptNumber = (lastAttempt?.AttemptNumber ?? 0) + 1;

    var attempt = new ApiPaymentAttempt
    {
        OrderId = id,
        InvoiceId = invoice.Id,
        Status = "pending",
        Amount = invoice.Total,
        FailureReason = "",
        TransactionId = "",
        AttemptNumber = attemptNumber,
        CreatedAt = DateTime.Now
    };

    invoice.Status = "locked";
    invoice.LockedAt = DateTime.Now;

    var paymentSucceeded = Random.Shared.Next(0, 100) > 20;
    if (paymentSucceeded)
    {
        attempt.Status = "success";
        attempt.TransactionId = $"txn_{DateTime.Now.Ticks}";
        order.Status = "Paid";
        order.PaymentStatus = "Paid";
        invoice.Status = "final";
    }
    else
    {
        attempt.Status = "failed";
        attempt.FailureReason = "Card declined - insufficient funds";
        order.Status = "PaymentFailed";
        order.PaymentStatus = "PaymentFailed";
        attempt.NextRetryAt = DateTime.Now.AddHours(6);
    }

    order.LastUpdatedAt = DateTime.Now;
    db.PaymentAttempts.Add(attempt);
    await db.SaveChangesAsync();

    return Results.Ok(new { status = attempt.Status, amount = attempt.Amount, transactionId = attempt.TransactionId });
});

app.MapPost("/api/orders/{id:int}/payment-method", async (int id, SavePaymentMethodRequest request, ApiDbContext db) =>
{
    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
    if (order is null)
    {
        return Results.NotFound();
    }

    if (string.IsNullOrWhiteSpace(request.CardToken) || string.IsNullOrWhiteSpace(request.CardLast4))
    {
        return Results.BadRequest(new { error = "Card details are required." });
    }

    var method = new ApiPaymentMethod
    {
        UserEmail = order.UserEmail,
        CardToken = request.CardToken,
        CardLast4 = request.CardLast4,
        CardBrand = request.CardBrand,
        ExpiryMonth = request.ExpiryMonth,
        ExpiryYear = request.ExpiryYear,
        IsDefault = true,
        IsVerified = false,
        CreatedAt = DateTime.Now,
        LastUsedAt = DateTime.Now
    };

    db.PaymentMethods.Add(method);
    await db.SaveChangesAsync();

    order.PaymentMethodId = method.Id;
    order.PaymentStatus = "PaymentMethodOnFile";
    order.Status = "PendingPickup";
    if (request.AcceptTerms)
    {
        order.TermsAccepted = true;
        order.TermsAcceptedAt = DateTime.UtcNow.ToString("o");
    }

    order.LastUpdatedAt = DateTime.Now;
    await db.SaveChangesAsync();

    return Results.Ok(new { paymentMethodId = method.Id });
});

app.MapPost("/api/orders/{id:int}/payment-method/update", async (int id, SavePaymentMethodRequest request, ApiDbContext db) =>
{
    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
    if (order is null)
    {
        return Results.NotFound();
    }

    if (string.IsNullOrWhiteSpace(request.CardToken) || string.IsNullOrWhiteSpace(request.CardLast4))
    {
        return Results.BadRequest(new { error = "Card details are required." });
    }

    var method = new ApiPaymentMethod
    {
        UserEmail = order.UserEmail,
        CardToken = request.CardToken,
        CardLast4 = request.CardLast4,
        CardBrand = request.CardBrand,
        ExpiryMonth = request.ExpiryMonth,
        ExpiryYear = request.ExpiryYear,
        IsDefault = true,
        IsVerified = false,
        CreatedAt = DateTime.Now,
        LastUsedAt = DateTime.Now
    };

    db.PaymentMethods.Add(method);
    await db.SaveChangesAsync();

    order.PaymentMethodId = method.Id;
    order.PaymentStatus = "PaymentMethodOnFile";
    order.LastUpdatedAt = DateTime.Now;
    await db.SaveChangesAsync();

    return Results.Ok(new { paymentMethodId = method.Id });
});

app.MapGet("/api/orders/{id:int}/invoice", async (int id, ApiDbContext db) =>
{
    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
    if (order is null)
    {
        return Results.NotFound();
    }

    ApiInvoice? invoice = null;
    if (order.InvoiceId.HasValue)
    {
        invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == order.InvoiceId.Value);
    }

    invoice ??= await db.Invoices.FirstOrDefaultAsync(i => i.OrderId == id);
    return invoice is null ? Results.NotFound() : Results.Ok(invoice);
});

app.MapPost("/api/orders/{id:int}/payment/retry", async (int id, ApiDbContext db) =>
{
    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
    if (order is null)
    {
        return Results.NotFound();
    }

    var invoice = order.InvoiceId.HasValue
        ? await db.Invoices.FirstOrDefaultAsync(i => i.Id == order.InvoiceId.Value)
        : await db.Invoices.FirstOrDefaultAsync(i => i.OrderId == id);

    if (invoice is null)
    {
        return Results.BadRequest(new { error = "Invoice not found." });
    }

    var lastAttempt = await db.PaymentAttempts
        .Where(pa => pa.OrderId == id)
        .OrderByDescending(pa => pa.CreatedAt)
        .FirstOrDefaultAsync();

    var attemptNumber = (lastAttempt?.AttemptNumber ?? 0) + 1;
    if (attemptNumber > 3)
    {
        return Results.BadRequest(new { error = "Maximum retry attempts reached." });
    }

    var attempt = new ApiPaymentAttempt
    {
        OrderId = id,
        InvoiceId = invoice.Id,
        Amount = invoice.Total,
        AttemptNumber = attemptNumber,
        CreatedAt = DateTime.Now,
        Status = "pending"
    };

    var paymentSucceeded = Random.Shared.Next(0, 100) > 30;
    if (paymentSucceeded)
    {
        attempt.Status = "success";
        attempt.TransactionId = $"txn_{DateTime.Now.Ticks}";
        order.Status = "Paid";
        order.PaymentStatus = "Paid";
        invoice.Status = "final";
    }
    else
    {
        attempt.Status = "failed";
        attempt.FailureReason = "Card declined - please update payment method";
        order.Status = "PaymentFailed";
        order.PaymentStatus = "PaymentFailed";

        if (attemptNumber < 3)
        {
            attempt.NextRetryAt = DateTime.Now.AddHours(24);
        }
    }

    order.LastUpdatedAt = DateTime.Now;
    db.PaymentAttempts.Add(attempt);
    await db.SaveChangesAsync();

    return Results.Ok(new { status = attempt.Status, attemptNumber = attempt.AttemptNumber, transactionId = attempt.TransactionId });
});

app.MapGet("/api/orders", async (string userEmail, ApiDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(userEmail))
    {
        return Results.BadRequest(new { error = "userEmail is required." });
    }

    var normalizedEmail = ApiHelpers.NormalizeEmail(userEmail);

    var orders = await db.Orders
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();

    var filtered = orders
        .Where(o => ApiHelpers.NormalizeEmail(o.UserEmail) == normalizedEmail)
        .ToList();

    return Results.Ok(filtered);
});

app.MapGet("/api/admin/orders", async (string? status, string? search, ApiDbContext db) =>
{
    var query = db.Orders.AsQueryable();

    if (!string.IsNullOrWhiteSpace(status) && status != "All")
    {
        query = query.Where(o => o.Status == status);
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
        var lowered = search.ToLower();
        query = query.Where(o =>
            o.UserEmail.ToLower().Contains(lowered) ||
            o.Id.ToString().Contains(lowered) ||
            o.Address.ToLower().Contains(lowered));
    }

    var orders = await query
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();

    return Results.Ok(orders);
});

app.MapPost("/api/admin/orders/{id:int}/status", async (int id, UpdateOrderStatusRequest request, ApiDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Status))
    {
        return Results.BadRequest(new { error = "Status is required." });
    }

    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
    if (order is null)
    {
        return Results.NotFound();
    }

    order.Status = request.Status;
    order.LastUpdatedAt = DateTime.Now;
    await db.SaveChangesAsync();

    return Results.Ok(new { id = order.Id, status = order.Status });
});

app.MapPost("/api/admin/orders/{id:int}/admin-notes", async (int id, UpdateAdminNotesRequest request, ApiDbContext db) =>
{
    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
    if (order is null)
    {
        return Results.NotFound();
    }

    order.AdminNotes = request.AdminNotes?.Trim() ?? "";
    order.LastUpdatedAt = DateTime.Now;
    await db.SaveChangesAsync();

    return Results.Ok(new { id = order.Id, adminNotes = order.AdminNotes });
});

app.MapPost("/api/admin/orders/{id:int}/payment-status", async (int id, UpdatePaymentStatusRequest request, ApiDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.PaymentStatus))
    {
        return Results.BadRequest(new { error = "PaymentStatus is required." });
    }

    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
    if (order is null)
    {
        return Results.NotFound();
    }

    order.PaymentStatus = request.PaymentStatus;
    order.LastUpdatedAt = DateTime.Now;
    await db.SaveChangesAsync();

    return Results.Ok(new { id = order.Id, paymentStatus = order.PaymentStatus });
});

app.MapDelete("/api/admin/orders/{id:int}", async (int id, ApiDbContext db) =>
{
    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
    if (order is null)
    {
        return Results.NotFound();
    }

    db.Orders.Remove(order);
    await db.SaveChangesAsync();
    return Results.Ok(new { id });
});

app.MapPost("/api/jobs/email/order-created", (OrderCreatedEmailRequest request) =>
{
    if (request.OrderId <= 0 || string.IsNullOrWhiteSpace(request.ToEmail) || string.IsNullOrWhiteSpace(request.ServiceType))
    {
        return Results.BadRequest(new { error = "OrderId, ToEmail, and ServiceType are required." });
    }

    var job = new QueuedEmailJob
    {
        JobId = Guid.NewGuid(),
        JobType = "order-created",
        ToEmail = request.ToEmail,
        OrderId = request.OrderId,
        ServiceType = request.ServiceType,
        ScheduledAt = request.ScheduledAt,
        Address = request.Address,
        CreatedAtUtc = DateTime.UtcNow
    };

    queue.Enqueue(job);
    return Results.Accepted($"/api/jobs/{job.JobId}", new { message = "Order-created email job queued.", jobId = job.JobId });
});

app.MapPost("/api/jobs/email/receipt", (ReceiptEmailRequest request) =>
{
    if (request.OrderId <= 0 || string.IsNullOrWhiteSpace(request.ToEmail))
    {
        return Results.BadRequest(new { error = "OrderId and ToEmail are required." });
    }

    var job = new QueuedEmailJob
    {
        JobId = Guid.NewGuid(),
        JobType = "receipt",
        ToEmail = request.ToEmail,
        OrderId = request.OrderId,
        ServiceType = request.ServiceType,
        Amount = request.Amount,
        TransactionId = request.TransactionId,
        Address = request.Address,
        CreatedAtUtc = DateTime.UtcNow
    };

    queue.Enqueue(job);
    return Results.Accepted($"/api/jobs/{job.JobId}", new { message = "Receipt email job queued.", jobId = job.JobId });
});

app.MapGet("/api/jobs/next", () =>
{
    if (!queue.TryDequeue(out var job))
    {
        return Results.NoContent();
    }

    return Results.Ok(job);
});

app.MapPost("/api/jobs/requeue", (QueuedEmailJob job) =>
{
    if (job.JobId == Guid.Empty || string.IsNullOrWhiteSpace(job.ToEmail) || string.IsNullOrWhiteSpace(job.JobType))
    {
        return Results.BadRequest(new { error = "Invalid job payload." });
    }

    queue.Enqueue(job);
    return Results.Accepted($"/api/jobs/{job.JobId}", new { message = "Job re-queued.", jobId = job.JobId });
});

app.MapPost("/api/jobs/{jobId:guid}/ack", (Guid jobId) => Results.Ok(new { message = "Job acknowledged.", jobId }));

app.Run();

public class QueuedEmailJob
{
    public Guid JobId { get; set; }
    public string JobType { get; set; } = "";
    public string ToEmail { get; set; } = "";
    public int OrderId { get; set; }
    public string ServiceType { get; set; } = "";
    public string ScheduledAt { get; set; } = "";
    public string Address { get; set; } = "";
    public decimal? Amount { get; set; }
    public string TransactionId { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

public record OrderCreatedEmailRequest(int OrderId, string ToEmail, string ServiceType, string ScheduledAt, string Address);

public record ReceiptEmailRequest(int OrderId, string ToEmail, string ServiceType, decimal? Amount, string TransactionId, string Address);

public record CreateOrderRequest(
    string UserEmail,
    string ServiceType,
    DateTime ScheduledAt,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? ZipCode,
    string? Notes);

public record SavePaymentMethodRequest(
    string CardToken,
    string CardLast4,
    string CardBrand,
    string ExpiryMonth,
    string ExpiryYear,
    bool AcceptTerms);

public record GenerateInvoiceRequest(
    decimal Subtotal,
    decimal TaxAmount,
    decimal DeliveryFee,
    string? LineItems);

public record UpdateOrderStatusRequest(string Status);

public record UpdateAdminNotesRequest(string? AdminNotes);

public record UpdatePaymentStatusRequest(string PaymentStatus);

public static class ApiHelpers
{
    public static string NormalizeEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        return new string(value
            .Trim()
            .Where(c => !char.IsWhiteSpace(c) && !char.IsControl(c) && CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.Format)
            .ToArray())
            .ToLowerInvariant();
    }

    public static string BuildAddress(string? addressLine1, string? addressLine2, string? city, string? state, string? zipCode)
    {
        var segments = new List<string>();

        if (!string.IsNullOrWhiteSpace(addressLine1))
        {
            segments.Add(addressLine1.Trim());
        }

        if (!string.IsNullOrWhiteSpace(addressLine2))
        {
            segments.Add(addressLine2.Trim());
        }

        var cityStateZip = string.Join(" ", new[]
        {
            string.IsNullOrWhiteSpace(city) ? "" : city.Trim() + ",",
            string.IsNullOrWhiteSpace(state) ? "" : state.Trim(),
            string.IsNullOrWhiteSpace(zipCode) ? "" : zipCode.Trim()
        }.Where(x => !string.IsNullOrWhiteSpace(x)));

        if (!string.IsNullOrWhiteSpace(cityStateZip))
        {
            segments.Add(cityStateZip.Replace(" ,", ","));
        }

        return string.Join(", ", segments);
    }

    public static string ResolveSharedDbPath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "laundry.db");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "laundry.db");
    }
}
