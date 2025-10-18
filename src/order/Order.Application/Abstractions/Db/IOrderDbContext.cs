using Order.Domain.Entities;
using Order.Domain.Outbox;

namespace Order.Application.Abstractions.Db;

public interface IOrderDbContext
{
    IQueryable<Domain.Entities.Order> Orders { get; }
    IQueryable<OrderLine> OrderLines { get; }
    
    IQueryable<OutboxMessage> Outbox { get; }

    Task AddAsync<T>(T entity, CancellationToken ct = default) where T : class;
    Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken ct = default) where T : class;

    Task AddOutboxAsync(OutboxMessage message, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}