using HealthResilienceDemo.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthResilienceDemo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly AppDbContext _db;
    public CustomerController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetCustomers() => Ok(await _db.Customers.ToListAsync());

    [HttpPost]
    public async Task<IActionResult> AddCustomer([FromBody] Customer c)
    {
        _db.Customers.Add(c);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCustomers), new { id = c.Id }, c);
    }
}
