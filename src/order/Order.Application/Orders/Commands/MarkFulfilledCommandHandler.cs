using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Application.Abstractions.Db;      
using Order.Application.Orders.Events;     
using Order.Domain.Outbox; 

namespace Order.Application.Orders.Commands;

public sealed class MarkFulfilledCommandHandler : IRequestHandler<MarkFulfilledCommand>
{
    private readonly IOrderDbContext _db;
    private readonly ILogger<MarkFulfilledCommandHandler> _log;

    public MarkFulfilledCommandHandler(IOrderDbContext db, ILogger<MarkFulfilledCommandHandler> log)
    {
        _db = db;
        _log = log;
    }

    public async Task Handle(MarkFulfilledCommand request, CancellationToken ct)
    {
        var order = await _db.Orders
            .Where(o => o.OrderId == request.OrderId)
            .FirstOrDefaultAsync(ct);

        if (order is null)
            throw new KeyNotFoundException($"Order not found: {request.OrderId}");

        var lines = await _db.OrderLines
            .Where(l => l.OrderId == order.OrderId)
            .ToListAsync(ct);

        order.MarkPaid();
        order.MarkFulfilled(request.FulfilledAtUtc.UtcDateTime);

        var evt = new OrderFulfilledEvent(
            OrderId: order.OrderId,
            CustomerId: order.CustomerId,
            FulfilledAtUtc: request.FulfilledAtUtc.ToUniversalTime(),
            Total: lines.Sum(l => l.Quantity * l.UnitPrice),
            Lines: lines.Select(l => new OrderFulfilledLine(l.ProductId, l.Quantity, l.UnitPrice)).ToList()
        );

        var outbox = new OutboxMessage
        {
            Type = nameof(OrderFulfilledEvent),
            Payload = JsonSerializer.Serialize(evt),
            OccurredAtUtc = DateTimeOffset.UtcNow.UtcDateTime
        };

        await _db.AddOutboxAsync(outbox, ct);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Order {OrderId} fulfilled; event enqueued to outbox (OutboxId={OutboxId}).",
            order.OrderId, outbox.Id);
    }
}
