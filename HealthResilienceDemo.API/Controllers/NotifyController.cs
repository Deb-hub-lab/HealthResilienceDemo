using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using HealthResilienceDemo.API.Services;

namespace HealthResilienceDemo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotifyController : ControllerBase
    {
        private readonly EmailService _emailService;

        public NotifyController(EmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendTestEmail([FromBody] EmailRequest request)
        {
            await _emailService.SendEmailAsync(request.To, request.Subject, request.Body);
            return Ok("Email sent successfully!");
        }
    }

    public class EmailRequest
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}
