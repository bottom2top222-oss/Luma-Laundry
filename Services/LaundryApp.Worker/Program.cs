using LaundryApp.Worker;

var builder = Host.CreateApplicationBuilder(args);

var apiBaseUrl = builder.Configuration["LayeredServices:ApiBaseUrl"] ?? "http://localhost:5080";
builder.Services.AddHttpClient("ApiClient", client =>
{
	client.BaseAddress = new Uri(apiBaseUrl);
	client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<WorkerEmailSender>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
