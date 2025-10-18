using MediatR;
using Order.Application.Abstractions.Db;

namespace Order.Application.Orders.Commands;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderDbContext _db;
    public CreateOrderCommandHandler(IOrderDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var order = Order.Domain.Entities.Order.Create(
            request.CustomerId,
            request.Lines.Select(l => (l.ProductId, l.Quantity, l.UnitPrice))
        );

        await _db.AddAsync(order, ct);
        await _db.SaveChangesAsync(ct);
        return order.OrderId;
    }
}