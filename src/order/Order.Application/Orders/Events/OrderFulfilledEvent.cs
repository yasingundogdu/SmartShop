namespace Order.Application.Orders.Events;

public sealed record OrderFulfilledEvent(
    Guid OrderId,
    Guid CustomerId,
    DateTimeOffset FulfilledAtUtc,
    decimal Total,
    IReadOnlyCollection<OrderFulfilledLine> Lines);

public sealed record OrderFulfilledLine(Guid ProductId, int Quantity, decimal UnitPrice);