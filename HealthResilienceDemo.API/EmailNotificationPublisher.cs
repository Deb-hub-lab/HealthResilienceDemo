using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace HealthResilienceDemo.API
{
    public class HealthCheckPublisherOptionsCustom
    {
        public int DelaySeconds { get; set; } = 5;
        public int PeriodSeconds { get; set; } = 30;
        public int MinimumSecondsBetweenFailureNotifications { get; set; } = 300;
    }

    public class EmailSmtpOptions
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string From { get; set; } = "";
        public string To { get; set; } = "";
    }

    public class EmailNotificationPublisher : IHealthCheckPublisher
    {
        private readonly EmailSmtpOptions _smtp;
        private readonly ILogger<EmailNotificationPublisher> _logger;
        private readonly HealthCheckPublisherOptionsCustom _options;

        // suppression: track last send per scope (global here)
        private static readonly ConcurrentDictionary<string, DateTime> _lastSent =
            new ConcurrentDictionary<string, DateTime>();

        public EmailNotificationPublisher(
            IOptions<EmailSmtpOptions> smtpOptions,
            IOptions<HealthCheckPublisherOptionsCustom> publisherOptions,
            ILogger<EmailNotificationPublisher> logger)
        {
            _smtp = smtpOptions.Value;
            _logger = logger;
            _options = publisherOptions.Value;
        }

        public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            try
            {
                // Only send when Unhealthy
                if (report.Status != HealthStatus.Unhealthy)
                {
                    _logger.LogInformation("Health status is {status} — no email sent.", report.Status);
                    return;
                }

                var now = DateTime.UtcNow;
                var key = "global-health-failure"; // change to endpoint-specific if needed

                var last = _lastSent.GetOrAdd(key, DateTime.MinValue);
                if ((now - last).TotalSeconds < _options.MinimumSecondsBetweenFailureNotifications)
                {
                    _logger.LogInformation("Skipping email; last sent {age} seconds ago.", (now - last).TotalSeconds);
                    return;
                }

                // Build message
                var subject = $"[ALERT] Health Checks Unhealthy - {report.Status}";
                var body = $"Health report at {DateTime.UtcNow:O}\n\nTotal duration: {report.TotalDuration}\n\n";
                body += string.Join("\n", report.Entries.Select(kvp =>
                {
                    var desc = kvp.Value.Description ?? "";
                    var exc = kvp.Value.Exception?.Message ?? "";
                    return $"{kvp.Key}: {kvp.Value.Status} - Duration: {kvp.Value.Duration} {(!string.IsNullOrEmpty(desc) ? $" - {desc}" : "")}{(!string.IsNullOrEmpty(exc) ? $" - Exception: {exc}" : "")}";
                }));

                await SendEmailAsync(subject, body, cancellationToken);

                // update last sent
                _lastSent[key] = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while publishing health report email.");
            }
        }

        private async Task SendEmailAsync(string subject, string body, CancellationToken cancellationToken)
        {
            using var client = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                EnableSsl = _smtp.EnableSsl,
                Credentials = new NetworkCredential(_smtp.Username, _smtp.Password)
            };

            var mail = new MailMessage(_smtp.From, _smtp.To, subject, body);
            // set plain text; for HTML set IsBodyHtml = true and body accordingly
            mail.IsBodyHtml = false;

            // Send asynchronously
            await client.SendMailAsync(mail, cancellationToken);
        }
    }
}
