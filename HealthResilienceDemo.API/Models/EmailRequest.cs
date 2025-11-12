namespace HealthResilienceDemo.API.Models
{
    public class EmailRequest
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = "Test Email from HealthCheck System";
        public string Body { get; set; } = "This is a test email.";
    }
}
