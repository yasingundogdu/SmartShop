using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Customer.Infrastructure.Persistence;
using Customer.Domain.Entities;

namespace Customer.Api.Startup;

public static class CustomerSeed
{
    private static readonly Guid CUST_DEMO = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CUST_EX   = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static async Task EnsureAsync(CustomerDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var demo = await db.Set<Domain.Entities.Customer>()
            .FirstOrDefaultAsync(x => x.Email == "demo@acme.test", ct);
        if (demo is null)
        {
            demo = Domain.Entities.Customer.Create("Acme Demo", "demo@acme.test");
            typeof(Domain.Entities.Customer).GetProperty(nameof(Domain.Entities.Customer.CustomerId))!
                .SetValue(demo, CUST_DEMO);
            db.Add(demo);
            logger.LogInformation("Seed(Customer): {Email}", demo.Email);
        }

        var ex = await db.Set<Domain.Entities.Customer>()
            .FirstOrDefaultAsync(x => x.Email == "info@example.com", ct);
        if (ex is null)
        {
            ex = Domain.Entities.Customer.Create("Example Ltd", "info@example.com");
            typeof(Domain.Entities.Customer).GetProperty(nameof(Domain.Entities.Customer.CustomerId))!
                .SetValue(ex, CUST_EX);
            db.Add(ex);
            logger.LogInformation("Seed(Customer): {Email}", ex.Email);
        }

        await db.SaveChangesAsync(ct);
    }
}