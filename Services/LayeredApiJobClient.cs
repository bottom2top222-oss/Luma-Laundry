using System.Net.Http.Json;
using LaundryApp.Models;

namespace LaundryApp.Services;

public class LayeredApiJobClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LayeredApiJobClient> _logger;

    public LayeredApiJobClient(HttpClient httpClient, ILogger<LayeredApiJobClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task QueueOrderCreatedEmailAsync(LaundryOrder order)
    {
        var payload = new
        {
            orderId = order.Id,
            toEmail = order.UserEmail,
            serviceType = order.ServiceType,
            scheduledAt = order.ScheduledAt.ToString("f"),
            address = order.GetDisplayAddress()
        };

        await PostJobAsync("/api/jobs/email/order-created", payload);
    }

    public async Task QueueReceiptEmailAsync(LaundryOrder order, Invoice? invoice, PaymentAttempt? paymentAttempt)
    {
        var payload = new
        {
            orderId = order.Id,
            toEmail = order.UserEmail,
            serviceType = order.ServiceType,
            amount = invoice?.Total ?? paymentAttempt?.Amount,
            transactionId = paymentAttempt?.TransactionId ?? "",
            address = order.GetDisplayAddress()
        };

        await PostJobAsync("/api/jobs/email/receipt", payload);
    }

    private async Task PostJobAsync(string path, object payload)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(path, payload);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to queue job at {Path}. Status={StatusCode}", path, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error queueing background job at {Path}", path);
        }
    }
}
