using System.Net;
using System.Net.Http.Json;

namespace LaundryApp.Worker;

public class Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory, WorkerEmailSender emailSender) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("LaundryApp.Worker started. Waiting for queued jobs...");
        var apiClient = httpClientFactory.CreateClient("ApiClient");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await apiClient.GetAsync("/api/jobs/next", stoppingToken);

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Worker job fetch failed with status {StatusCode}", response.StatusCode);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                var job = await response.Content.ReadFromJsonAsync<QueuedEmailJob>(cancellationToken: stoppingToken);
                if (job == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                var sent = job.JobType switch
                {
                    "order-created" => await emailSender.SendOrderCreatedEmailAsync(job),
                    "receipt" => await emailSender.SendReceiptEmailAsync(job),
                    _ => false
                };

                if (!sent)
                {
                    logger.LogWarning("Email sending failed for job {JobId}. Re-queueing.", job.JobId);
                    await apiClient.PostAsJsonAsync("/api/jobs/requeue", job, stoppingToken);
                }
                else
                {
                    await apiClient.PostAsync($"/api/jobs/{job.JobId}/ack", null, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Worker loop error");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
