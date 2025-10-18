using Order.Application.Orders.Events;

namespace Order.Application.Abstractions.Messaging;

public interface IEventPublisher
{
    Task PublishOrderFulfilledAsync(OrderFulfilledEvent evt, CancellationToken ct = default);
    Task PublishRawAsync(string exchange, string routingKey, string bodyUtf8, CancellationToken ct);
}