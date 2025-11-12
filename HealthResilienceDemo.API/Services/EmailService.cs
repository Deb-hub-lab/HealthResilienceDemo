using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace HealthResilienceDemo.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpSettings = _config.GetSection("SmtpSettings");
            string host = smtpSettings["Host"];
            int port = int.Parse(smtpSettings["Port"]);
            string fromEmail = smtpSettings["FromEmail"];
            string password = smtpSettings["Password"];

            // 👇 Create the email message with display name
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Health Monitor", fromEmail));  // ✅ Display Name here
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(fromEmail, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
