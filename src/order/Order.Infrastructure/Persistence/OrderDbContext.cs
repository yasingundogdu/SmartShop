using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Order.Application.Abstractions.Db;
using Order.Domain.Entities;
using Order.Domain.Outbox;

namespace Order.Infrastructure.Persistence;

public class OrderDbContext : DbContext, IOrderDbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }
    public DbSet<Domain.Entities.Order> OrderSet => Set<Domain.Entities.Order>();
    public DbSet<OrderLine> OrderLineSet => Set<OrderLine>();
    public DbSet<OutboxMessage> OutboxSet => Set<OutboxMessage>();
    
    public IQueryable<OutboxMessage> Outbox => OutboxSet.AsQueryable();

    public IQueryable<Domain.Entities.Order> Orders => OrderSet.AsQueryable();
    public IQueryable<OrderLine> OrderLines => OrderLineSet.AsQueryable();

    public async Task AddAsync<T>(T entity, CancellationToken ct = default) where T : class
        => await Set<T>().AddAsync(entity, ct);

    public async Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken ct = default) where T : class
        => await Set<T>().AddRangeAsync(entities, ct);

    public Task AddOutboxAsync(OutboxMessage message, CancellationToken ct = default)
    {
        OutboxSet.Add(message);
        return Task.CompletedTask;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => base.SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("order");

        modelBuilder.Entity<Domain.Entities.Order>(b =>
        {
            b.ToTable("order");
            b.HasKey(x => x.OrderId);
            b.Property(x => x.OrderId).HasColumnName("order_id");
            b.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            b.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
            b.Property(x => x.PaidAt).HasColumnName("paid_at").HasColumnType("timestamp with time zone");
            b.Property(x => x.FulfilledAt).HasColumnName("fulfilled_at").HasColumnType("timestamp with time zone");
            b.Property(x => x.CancelledAt).HasColumnName("cancelled_at").HasColumnType("timestamp with time zone");

            b.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(l => l.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderLine>(b =>
        {
            b.ToTable("order_line");
            b.HasKey(l => new { l.OrderId, l.ProductId });
            b.Property(l => l.OrderId).HasColumnName("order_id");
            b.Property(l => l.ProductId).HasColumnName("product_id");
            b.Property(l => l.Quantity).HasColumnName("quantity").IsRequired();
            b.Property(l => l.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(18,2)").IsRequired();
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_message");
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(200).IsRequired();
            e.Property(x => x.Payload).IsRequired();
            e.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.ProcessedAtUtc).HasColumnName("processed_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RetryCount).HasColumnName("retry_count");
            e.Property(x => x.Error).HasColumnName("error");
            e.Property(x => x.Status).HasColumnName("status").HasConversion<int>();
        });
    }
}
