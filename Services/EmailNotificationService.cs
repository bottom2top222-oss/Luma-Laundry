using System.Net;
using System.Net.Mail;
using LaundryApp.Models;
using Microsoft.Extensions.Configuration;

namespace LaundryApp.Services;

public class EmailNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(IConfiguration configuration, ILogger<EmailNotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendOrderCreatedEmailAsync(LaundryOrder order)
    {
        var subject = $"LUMA Order Confirmation #{order.Id}";
        var body = $@"
            <h2>Your order has been created</h2>
            <p>Thanks for scheduling with LUMA.</p>
            <p><strong>Order #:</strong> {order.Id}</p>
            <p><strong>Service:</strong> {WebUtility.HtmlEncode(order.ServiceType)}</p>
            <p><strong>Scheduled At:</strong> {order.ScheduledAt:f}</p>
            <p><strong>Address:</strong> {WebUtility.HtmlEncode(order.GetDisplayAddress())}</p>
            <p><strong>Status:</strong> {WebUtility.HtmlEncode(order.Status)}</p>
            <p>We will notify you when your order progresses.</p>";

        return await SendEmailAsync(order.UserEmail, subject, body);
    }

    public async Task<bool> SendPaymentReceiptEmailAsync(LaundryOrder order, Invoice? invoice, PaymentAttempt? paymentAttempt)
    {
        var totalPaid = invoice?.Total ?? paymentAttempt?.Amount ?? 0m;
        var transactionId = string.IsNullOrWhiteSpace(paymentAttempt?.TransactionId)
            ? $"TXN-{order.Id:D8}"
            : paymentAttempt!.TransactionId;

        var subject = $"LUMA Payment Receipt #{order.Id}";
        var body = $@"
            <h2>Payment Received</h2>
            <p>Your payment was successful.</p>
            <p><strong>Receipt #:</strong> RCP-{order.Id:D6}</p>
            <p><strong>Order #:</strong> {order.Id}</p>
            <p><strong>Service:</strong> {WebUtility.HtmlEncode(order.ServiceType)}</p>
            <p><strong>Payment Status:</strong> {WebUtility.HtmlEncode(order.PaymentStatus)}</p>
            <p><strong>Transaction ID:</strong> {WebUtility.HtmlEncode(transactionId)}</p>
            <p><strong>Total Paid:</strong> ${totalPaid:F2}</p>
            <p><strong>Address:</strong> {WebUtility.HtmlEncode(order.GetDisplayAddress())}</p>
            <p>Thank you for choosing LUMA.</p>";

        return await SendEmailAsync(order.UserEmail, subject, body);
    }

    private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var host = _configuration["Email:SmtpHost"];
        var fromAddress = _configuration["Email:FromAddress"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogWarning("Email not sent because SMTP is not configured. To={ToEmail}, Subject={Subject}", toEmail, subject);
            return false;
        }

        var fromName = _configuration["Email:FromName"] ?? "LUMA Laundry";
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        var port = int.TryParse(_configuration["Email:SmtpPort"], out var parsedPort) ? parsedPort : 587;
        var enableSsl = bool.TryParse(_configuration["Email:EnableSsl"], out var parsedSsl) ? parsedSsl : true;

        using var message = new MailMessage();
        message.From = new MailAddress(fromAddress, fromName);
        message.To.Add(toEmail);
        message.Subject = subject;
        message.Body = htmlBody;
        message.IsBodyHtml = true;

        using var smtpClient = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl
        };

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            smtpClient.Credentials = new NetworkCredential(username, password);
        }

        try
        {
            await smtpClient.SendMailAsync(message);
            _logger.LogInformation("Email sent. To={ToEmail}, Subject={Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email. To={ToEmail}, Subject={Subject}", toEmail, subject);
            return false;
        }
    }
}