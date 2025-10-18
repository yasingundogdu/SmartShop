namespace Order.Application.Orders.Dtos;

public record FulfilledOrderLineDto(Guid ProductId, string? ProductName, string? Sku, int Quantity, decimal UnitPrice, decimal LineTotal);
public record FulfilledOrderDto(
    Guid OrderId,
    Guid CustomerId,
    string? CustomerName,
    string? CustomerEmail,
    DateTime? FulfilledAt,
    decimal Total,
    IReadOnlyCollection<FulfilledOrderLineDto> Lines
);

public record PagedResult<T>(int Total, int Page, int PageSize, IReadOnlyCollection<T> Items);