using HealthResilienceDemo.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthResilienceDemo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class customerController : ControllerBase
{
    private readonly AppDbContext _db;
    public customerController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Getcustomers() => Ok(await _db.customers.ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Addcustomer([FromBody] customer c)
    {
        _db.customers.Add(c);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Getcustomers), new { id = c.Id }, c);
    }
}
