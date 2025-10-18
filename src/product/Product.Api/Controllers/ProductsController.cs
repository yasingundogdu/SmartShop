using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Product.Infrastructure.Persistence;

namespace Product.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(ProductDbContext db) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await db.Products
            .AsNoTracking()
            .Where(p => p.ProductId == id)
            .Select(p => new
            {
                p.ProductId,
                p.Sku,
                p.Name
            })
            .FirstOrDefaultAsync(ct);

        return dto is null ? NotFound() : Ok(dto);
    }
}