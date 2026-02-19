using System.Net;
using System.Net.Mail;

namespace LaundryApp.Worker;

public class WorkerEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WorkerEmailSender> _logger;

    public WorkerEmailSender(IConfiguration configuration, ILogger<WorkerEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendOrderCreatedEmailAsync(QueuedEmailJob job)
    {
        var subject = $"LUMA Order Confirmation #{job.OrderId}";
        var body = $@"
            <h2>Your order has been created</h2>
            <p><strong>Order #:</strong> {job.OrderId}</p>
            <p><strong>Service:</strong> {WebUtility.HtmlEncode(job.ServiceType)}</p>
            <p><strong>Scheduled At:</strong> {WebUtility.HtmlEncode(job.ScheduledAt)}</p>
            <p><strong>Address:</strong> {WebUtility.HtmlEncode(job.Address)}</p>
            <p>Thanks for scheduling with LUMA.</p>";

        return await SendEmailAsync(job.ToEmail, subject, body);
    }

    public async Task<bool> SendReceiptEmailAsync(QueuedEmailJob job)
    {
        var amount = job.Amount ?? 0m;
        var transactionId = string.IsNullOrWhiteSpace(job.TransactionId) ? $"TXN-{job.OrderId:D8}" : job.TransactionId;

        var subject = $"LUMA Payment Receipt #{job.OrderId}";
        var body = $@"
            <h2>Payment Received</h2>
            <p><strong>Receipt #:</strong> RCP-{job.OrderId:D6}</p>
            <p><strong>Order #:</strong> {job.OrderId}</p>
            <p><strong>Service:</strong> {WebUtility.HtmlEncode(job.ServiceType)}</p>
            <p><strong>Transaction ID:</strong> {WebUtility.HtmlEncode(transactionId)}</p>
            <p><strong>Total Paid:</strong> ${amount:F2}</p>
            <p><strong>Address:</strong> {WebUtility.HtmlEncode(job.Address)}</p>
            <p>Thank you for choosing LUMA.</p>";

        return await SendEmailAsync(job.ToEmail, subject, body);
    }

    private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var host = _configuration["Email:SmtpHost"];
        var fromAddress = _configuration["Email:FromAddress"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogWarning("Worker email skipped: SMTP not configured. To={ToEmail}, Subject={Subject}", toEmail, subject);
            return true;
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
            _logger.LogInformation("Worker sent email. To={ToEmail}, Subject={Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker failed to send email. To={ToEmail}, Subject={Subject}", toEmail, subject);
            return false;
        }
    }
}
