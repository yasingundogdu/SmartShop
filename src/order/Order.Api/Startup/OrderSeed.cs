using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Infrastructure.Persistence;
using Order.Domain.Entities;

namespace Order.Api.Startup;

public static class OrderSeed
{
    private static readonly Guid ORDER_1   = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid CUST_DEMO = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PROD_1    = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PROD_2    = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task EnsureAsync(OrderDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var anyOrder = await db.Set<Domain.Entities.Order>().AnyAsync(ct);
        if (anyOrder)
        {
            logger.LogInformation("Seed(Order): orders found, skipping.");
            return;
        }

        var order = Domain.Entities.Order.Create(
            CUST_DEMO,
            new (Guid productId, int quantity, decimal unitPrice)[]
            {
                (PROD_1, 1, 99.90m),
                (PROD_2, 1, 199.90m)
            });

        typeof(Domain.Entities.Order).GetProperty(nameof(Domain.Entities.Order.OrderId))!
            .SetValue(order, ORDER_1);

        db.Add(order);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seed(Order): demo order created for customer {CustomerId}", CUST_DEMO);
    }
}