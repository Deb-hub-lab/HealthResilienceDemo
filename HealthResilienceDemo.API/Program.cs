using HealthChecks.UI.Client;
using HealthResilienceDemo.API;
using HealthResilienceDemo.API.Data;
using HealthResilienceDemo.API.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ Core Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<EmailService>();


// Bind SMTP and publisher config from appsettings.json
builder.Services.Configure<EmailSmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<HealthCheckPublisherOptionsCustom>(builder.Configuration.GetSection("HealthCheckPublisher"));

// Register the publisher as a singleton that will be called by the HealthCheckPublisherService
builder.Services.AddSingleton<IHealthCheckPublisher, EmailNotificationPublisher>();

// Configure the built-in publisher background service timing
builder.Services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckPublisherOptions>(options =>
{
    // Delay first run after app start
    options.Delay = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("HealthCheckPublisher:DelaySeconds", 5));
    // Period between runs
    options.Period = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("HealthCheckPublisher:PeriodSeconds", 30));
    // Run on change only - set to false to always run every period
    options.Predicate = report => true;
});

// 2️⃣ Database Context (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

// 3️⃣ Health Checks
builder.Services.AddHealthChecks()
    // PostgreSQL connection check
    .AddNpgSql(builder.Configuration.GetConnectionString("PostgresConnection")!,
        name: "PostgreSQL",
        failureStatus: HealthStatus.Unhealthy)
    // Redis connection check
    .AddRedis(builder.Configuration.GetConnectionString("RedisConnection")!,
        name: "Redis Cache",
        failureStatus: HealthStatus.Unhealthy)
    // External API connectivity check
    .AddUrlGroup(
        new Uri("https://jsonplaceholder.typicode.com/postsdd"),
        name: "External API",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "externalapi" })
    // Self check
    .AddCheck("Self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

// 4️⃣ HealthChecks UI (dashboard)
builder.Services.AddHealthChecksUI(options =>
{
    options.SetEvaluationTimeInSeconds(15);
    options.MaximumHistoryEntriesPerEndpoint(60);
    options.AddHealthCheckEndpoint("HealthCheck API Test", "/health/ready");
})
.AddSqliteStorage("Data Source=healthchecksdb");

// 5️⃣ HttpClient with Polly resilience
builder.Services.AddHttpClient("ExternalAPI", client =>
{
    client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
})
.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2)))
.AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

// 6️⃣ Build app
var app = builder.Build();

// 7️⃣ Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

// 8️⃣ Health Endpoints
// Live (self only)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Ready (all dependencies)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// UI Dashboard
app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
    options.ApiPath = "/health-ui-api";
});

app.Run();
