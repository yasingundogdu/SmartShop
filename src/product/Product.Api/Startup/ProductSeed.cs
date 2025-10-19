using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Product.Infrastructure.Persistence;
using Product.Domain.Entities;

namespace Product.Api.Startup;

public static class ProductSeed
{
    private static readonly Guid PROD_1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PROD_2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task EnsureAsync(ProductDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var p1 = await db.Set<Domain.Entities.Product>().FirstOrDefaultAsync(x => x.Sku == "SKU-1001", ct);
        if (p1 is null)
        {
            p1 = Domain.Entities.Product.Create("SKU-1001", "Starter Pack", 99.90m, "Initial seeded product");
            typeof(Domain.Entities.Product).GetProperty(nameof(Domain.Entities.Product.ProductId))!
                .SetValue(p1, PROD_1);
            db.Add(p1);
            logger.LogInformation("Seed(Product): {Sku}", "SKU-1001");
        }

        var p2 = await db.Set<Domain.Entities.Product>().FirstOrDefaultAsync(x => x.Sku == "SKU-1002", ct);
        if (p2 is null)
        {
            p2 = Domain.Entities.Product.Create("SKU-1002", "Pro Pack", 199.90m, "Initial seeded product");
            typeof(Domain.Entities.Product).GetProperty(nameof(Domain.Entities.Product.ProductId))!
                .SetValue(p2, PROD_2);
            db.Add(p2);
            logger.LogInformation("Seed(Product): {Sku}", "SKU-1002");
        }

        await db.SaveChangesAsync(ct);
    }
}