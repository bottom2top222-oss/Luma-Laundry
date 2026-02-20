using LaundryApp.Models;
using LaundryApp.Data;
using Microsoft.EntityFrameworkCore;

namespace LaundryApp.Services;

public class PaymentService
{
    private readonly LaundryAppDbContext _context;

    public PaymentService(LaundryAppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Save a payment method for a customer
    /// </summary>
    public async Task<PaymentMethod> SavePaymentMethodAsync(string userEmail, string cardToken, string cardLast4, 
        string cardBrand, string expiryMonth, string expiryYear, bool isDefault = false)
    {
        // If this is the first card or marked as default, set as default
        var existingDefault = _context.PaymentMethods
            .FirstOrDefault(pm => pm.UserEmail == userEmail && pm.IsDefault);

        var method = new PaymentMethod
        {
            UserEmail = userEmail,
            CardToken = cardToken,
            CardLast4 = cardLast4,
            CardBrand = cardBrand,
            ExpiryMonth = expiryMonth,
            ExpiryYear = expiryYear,
            IsDefault = isDefault || existingDefault == null,
            IsVerified = false,
            CreatedAt = DateTime.Now
        };

        _context.PaymentMethods.Add(method);
        await _context.SaveChangesAsync();

        return method;
    }

    /// <summary>
    /// Generate invoice when order is ready (after processing)
    /// </summary>
    public async Task<Invoice> GenerateInvoiceAsync(int orderId, decimal subtotal, decimal taxAmount = 0, 
        decimal deliveryFee = 0, string? lineItems = null)
    {
        var order = _context.Orders.Find(orderId);
        if (order == null)
            throw new Exception("Order not found");

        var total = subtotal + taxAmount + deliveryFee;

        var invoice = new Invoice
        {
            OrderId = orderId,
            Status = "draft",
            SubTotal = subtotal,
            TaxAmount = taxAmount,
            DeliveryFee = deliveryFee,
            Tip = 0,
            Total = total,
            LineItems = lineItems ?? "[]",
            CreatedAt = DateTime.Now,
            FinalizedAt = DateTime.Now
        };

        _context.Invoices.Add(invoice);
        order.InvoiceId = invoice.Id;
        order.Status = "Quoted";
        order.PaymentStatus = "ApprovalRequired";
        await _context.SaveChangesAsync();

        return invoice;
    }

    /// <summary>
    /// Lock invoice and attempt payment
    /// </summary>
    public async Task<PaymentAttempt> AttemptPaymentAsync(int orderId, decimal amount, int paymentMethodId)
    {
        var order = _context.Orders.Include(o => o.Invoice).FirstOrDefault(o => o.Id == orderId);
        if (order == null)
            throw new Exception("Order not found");

        if (order.Invoice == null)
            throw new Exception("Invoice not generated");

        // Lock the invoice
        order.Invoice.Status = "locked";
        order.Invoice.LockedAt = DateTime.Now;

        var attempt = new PaymentAttempt
        {
            OrderId = orderId,
            InvoiceId = order.InvoiceId,
            Status = "pending",
            Amount = amount,
            FailureReason = "",
            TransactionId = "",
            AttemptNumber = 1,
            CreatedAt = DateTime.Now
        };

        // Mock payment processing - in real app, integrate with Stripe/PayPal
        // Simulate 80% success rate
        var random = new Random();
        bool paymentSucceeded = random.Next(0, 100) > 20;

        if (paymentSucceeded)
        {
            attempt.Status = "success";
            attempt.TransactionId = $"txn_{DateTime.Now.Ticks}";
            order.Status = "Paid";
            order.PaymentStatus = "Paid";
            order.Invoice.Status = "final";
        }
        else
        {
            attempt.Status = "failed";
            attempt.FailureReason = "Card declined - insufficient funds";
            order.Status = "PaymentFailed";
            order.PaymentStatus = "PaymentFailed";
            
            // Schedule retry for 6 hours
            attempt.NextRetryAt = DateTime.Now.AddHours(6);
        }

        _context.PaymentAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        return attempt;
    }

    /// <summary>
    /// Retry failed payment
    /// </summary>
    public async Task<PaymentAttempt> RetryPaymentAsync(int orderId)
    {
        var order = _context.Orders.Include(o => o.Invoice).FirstOrDefault(o => o.Id == orderId);
        if (order == null)
            throw new Exception("Order not found");

        if (order.Invoice == null)
            throw new Exception("Invoice not found");

        // Get last attempt
        var lastAttempt = _context.PaymentAttempts
            .Where(pa => pa.OrderId == orderId)
            .OrderByDescending(pa => pa.CreatedAt)
            .FirstOrDefault();

        int attemptNumber = lastAttempt?.AttemptNumber + 1 ?? 1;

        // Max 3 retries
        if (attemptNumber > 3)
            throw new Exception("Maximum retry attempts reached");

        var newAttempt = new PaymentAttempt
        {
            OrderId = orderId,
            InvoiceId = order.InvoiceId,
            Status = "pending",
            Amount = order.Invoice.Total,
            FailureReason = "",
            TransactionId = "",
            AttemptNumber = attemptNumber,
            CreatedAt = DateTime.Now
        };

        // Mock retry - 70% success on retry
        var random = new Random();
        bool paymentSucceeded = random.Next(0, 100) > 30;

        if (paymentSucceeded)
        {
            newAttempt.Status = "success";
            newAttempt.TransactionId = $"txn_{DateTime.Now.Ticks}";
            order.Status = "Paid";
            order.PaymentStatus = "Paid";
            order.Invoice.Status = "final";
        }
        else
        {
            newAttempt.Status = "failed";
            newAttempt.FailureReason = "Card declined - please update payment method";
            
            if (attemptNumber < 3)
                newAttempt.NextRetryAt = DateTime.Now.AddHours(24);
        }

        _context.PaymentAttempts.Add(newAttempt);
        await _context.SaveChangesAsync();

        return newAttempt;
    }

    /// <summary>
    /// Close a paid order
    /// </summary>
    public async Task CloseOrderAsync(int orderId)
    {
        var order = _context.Orders.Find(orderId);
        if (order == null)
            throw new Exception("Order not found");

        if (order.PaymentStatus != "paid")
            throw new Exception("Order must be paid before closing");

        order.Status = "Closed";
        order.ClosedAt = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get payment history for an order
    /// </summary>
    public List<PaymentAttempt> GetPaymentHistory(int orderId)
    {
        return _context.PaymentAttempts
            .Where(pa => pa.OrderId == orderId)
            .OrderByDescending(pa => pa.CreatedAt)
            .ToList();
    }
}
