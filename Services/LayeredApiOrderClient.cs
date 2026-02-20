using System.Net.Http.Json;
using LaundryApp.Models;

namespace LaundryApp.Services;

public class LayeredApiOrderClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LayeredApiOrderClient> _logger;

    public LayeredApiOrderClient(HttpClient httpClient, ILogger<LayeredApiOrderClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<int?> CreateOrderAsync(LaundryOrder order)
    {
        try
        {
            var payload = new
            {
                userEmail = order.UserEmail,
                serviceType = order.ServiceType,
                scheduledAt = order.ScheduledAt,
                addressLine1 = order.AddressLine1,
                addressLine2 = order.AddressLine2,
                city = order.City,
                state = order.State,
                zipCode = order.ZipCode,
                notes = order.Notes
            };

            var response = await _httpClient.PostAsJsonAsync("/api/orders", payload);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to create order via API. Status={StatusCode}", response.StatusCode);
                return null;
            }

            var created = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
            return created?.OrderId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calling API to create order");
            return null;
        }
    }

    public async Task<List<LaundryOrder>?> GetUserOrdersAsync(string userEmail)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/orders?userEmail={Uri.EscapeDataString(userEmail)}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch user orders via API. Status={StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<List<LaundryOrder>>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calling API to fetch user orders");
            return null;
        }
    }

    public async Task<List<LaundryOrder>?> GetAdminOrdersAsync(string? status = null, string? search = null)
    {
        try
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query.Add($"status={Uri.EscapeDataString(status)}");
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query.Add($"search={Uri.EscapeDataString(search)}");
            }

            var suffix = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
            var response = await _httpClient.GetAsync($"/api/admin/orders{suffix}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch admin orders via API. Status={StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<List<LaundryOrder>>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calling API to fetch admin orders");
            return null;
        }
    }

    public async Task<LaundryOrder?> GetOrderAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/orders/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<LaundryOrder>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calling API to fetch order {OrderId}", id);
            return null;
        }
    }

    public async Task<bool> SavePaymentMethodAsync(int orderId, string cardToken, string cardLast4, string cardBrand, string expiryMonth, string expiryYear, bool acceptTerms)
    {
        var payload = new
        {
            cardToken,
            cardLast4,
            cardBrand,
            expiryMonth,
            expiryYear,
            acceptTerms
        };

        return await PostAsync($"/api/orders/{orderId}/payment-method", payload);
    }

    public async Task<bool> UpdatePaymentMethodAsync(int orderId, string cardToken, string cardLast4, string cardBrand, string expiryMonth, string expiryYear)
    {
        var payload = new
        {
            cardToken,
            cardLast4,
            cardBrand,
            expiryMonth,
            expiryYear,
            acceptTerms = false
        };

        return await PostAsync($"/api/orders/{orderId}/payment-method/update", payload);
    }

    public async Task<Invoice?> GetInvoiceAsync(int orderId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/orders/{orderId}/invoice");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<Invoice>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calling API to fetch invoice for order {OrderId}", orderId);
            return null;
        }
    }

    public async Task<(bool success, string status)> RetryPaymentAsync(int orderId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/orders/{orderId}/payment/retry", null);
            if (!response.IsSuccessStatusCode)
            {
                return (false, "failed");
            }

            var payload = await response.Content.ReadFromJsonAsync<RetryResponse>();
            return (true, payload?.Status ?? "pending");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calling API to retry payment for order {OrderId}", orderId);
            return (false, "failed");
        }
    }

    public async Task<(bool success, int? invoiceId)> GenerateInvoiceAsync(int orderId, decimal subtotal, decimal taxAmount, decimal deliveryFee, string lineItems)
    {
        var payload = new
        {
            subtotal,
            taxAmount,
            deliveryFee,
            lineItems
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/orders/{orderId}/invoice/generate", payload);
            if (!response.IsSuccessStatusCode)
            {
                return (false, null);
            }

            var result = await response.Content.ReadFromJsonAsync<GenerateInvoiceResponse>();
            return (true, result?.InvoiceId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calling API to generate invoice for order {OrderId}", orderId);
            return (false, null);
        }
    }

    public async Task<(bool success, string status, decimal amount, string transactionId)> AttemptPaymentAsync(int orderId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/orders/{orderId}/payment/attempt", null);
            if (!response.IsSuccessStatusCode)
            {
                return (false, "failed", 0m, "");
            }

            var payload = await response.Content.ReadFromJsonAsync<AttemptPaymentResponse>();
            return (true, payload?.Status ?? "pending", payload?.Amount ?? 0m, payload?.TransactionId ?? "");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calling API to attempt payment for order {OrderId}", orderId);
            return (false, "failed", 0m, "");
        }
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
    {
        var payload = new { status };
        return await PostAsync($"/api/admin/orders/{orderId}/status", payload);
    }

    public async Task<bool> UpdateAdminNotesAsync(int orderId, string adminNotes)
    {
        var payload = new { adminNotes };
        return await PostAsync($"/api/admin/orders/{orderId}/admin-notes", payload);
    }

    public async Task<bool> UpdatePaymentStatusAsync(int orderId, string paymentStatus)
    {
        var payload = new { paymentStatus };
        return await PostAsync($"/api/admin/orders/{orderId}/payment-status", payload);
    }

    public async Task<bool> DeleteOrderAsync(int orderId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/admin/orders/{orderId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error deleting order via API. OrderId={OrderId}", orderId);
            return false;
        }
    }

    private async Task<bool> PostAsync(string path, object payload)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(path, payload);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error posting to API path {Path}", path);
            return false;
        }
    }

    private class CreateOrderResponse
    {
        public int OrderId { get; set; }
    }

    private class RetryResponse
    {
        public string Status { get; set; } = "";
    }

    private class GenerateInvoiceResponse
    {
        public int InvoiceId { get; set; }
    }

    private class AttemptPaymentResponse
    {
        public string Status { get; set; } = "";
        public decimal Amount { get; set; }
        public string TransactionId { get; set; } = "";
    }
}
