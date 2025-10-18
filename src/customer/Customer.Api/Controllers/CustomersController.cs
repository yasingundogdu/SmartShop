using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Customer.Infrastructure.Persistence;

namespace Customer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController(CustomerDbContext db) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await db.Customers
            .AsNoTracking()
            .Where(c => c.CustomerId == id)
            .Select(c => new
            {
                c.CustomerId,
                c.Name,
                c.Email
            })
            .FirstOrDefaultAsync(ct);

        return dto is null ? NotFound() : Ok(dto);
    }
}