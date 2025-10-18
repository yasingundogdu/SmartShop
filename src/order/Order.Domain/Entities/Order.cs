namespace Order.Domain.Entities;

public enum OrderStatus { Created = 1, Paid = 2, Fulfilled = 3, Cancelled = 4 }

public class Order
{
    public Guid OrderId { get; private set; } = Guid.NewGuid();
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Created;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; private set; }
    public DateTime? FulfilledAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private readonly List<OrderLine> _lines = new();
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public static Order Create(Guid customerId, IEnumerable<(Guid productId, int quantity, decimal unitPrice)> items)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("customerId required");
        var o = new Order { CustomerId = customerId };
        foreach (var (productId, qty, price) in items)
            o.AddLine(productId, qty, price);
        return o;
    }

    public void AddLine(Guid productId, int quantity, decimal unitPrice)
    {
        if (productId == Guid.Empty) throw new ArgumentException("productId required");
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));
        _lines.Add(new OrderLine(OrderId, productId, quantity, unitPrice));
    }

    public decimal Total => _lines.Sum(l => l.Total);

    public void MarkPaid() { Status = OrderStatus.Paid; PaidAt = DateTime.UtcNow; }
    public void MarkFulfilled(DateTime? at = null) { Status = OrderStatus.Fulfilled; FulfilledAt = at ?? DateTime.UtcNow; }
    public void Cancel() { Status = OrderStatus.Cancelled; CancelledAt = DateTime.UtcNow; }
}

public class OrderLine
{
    private OrderLine() { }

    public OrderLine(Guid orderId, Guid productId, int quantity, decimal unitPrice)
    {
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Total => Quantity * UnitPrice;
}