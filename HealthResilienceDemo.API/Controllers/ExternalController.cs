using Microsoft.AspNetCore.Mvc;

namespace HealthResilienceDemo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExternalController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    public ExternalController(IHttpClientFactory factory) => _httpClientFactory = factory;

    [HttpGet("posts")]
    public async Task<IActionResult> GetExternalPosts()
    {
        var client = _httpClientFactory.CreateClient("ExternalService");
        var response = await client.GetAsync("posts");
        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }
}
