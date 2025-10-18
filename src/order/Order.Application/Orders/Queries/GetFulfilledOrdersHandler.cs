using MediatR;
using Microsoft.EntityFrameworkCore;         
using Order.Application.Abstractions.Db;         
using Order.Application.Orders.Dtos;
using Order.Domain.Entities;                    
using Refit;

namespace Order.Application.Orders.Queries
{
    public class GetFulfilledOrdersHandler
        : IRequestHandler<GetFulfilledOrdersQuery, PagedResult<FulfilledOrderDto>>
    {
        private readonly IOrderDbContext _db;   
        private readonly ICustomerClient _customers;
        private readonly IProductClient _products;

        public GetFulfilledOrdersHandler(
            IOrderDbContext db,
            ICustomerClient customers,
            IProductClient products)
        {
            _db = db;
            _customers = customers;
            _products = products;
        }

        public async Task<PagedResult<FulfilledOrderDto>> Handle(
            GetFulfilledOrdersQuery request,
            CancellationToken ct)
        {
            var fromUtc = request.From.UtcDateTime;
            var toUtc   = request.To.UtcDateTime;

            var baseQuery = _db.Orders
                .Where(o =>
                    o.Status == OrderStatus.Fulfilled &&
                    o.FulfilledAt != null &&
                    o.FulfilledAt >= fromUtc &&
                    o.FulfilledAt <= toUtc)
                .OrderBy(o => o.FulfilledAt);

            var total = await baseQuery.CountAsync(ct);

            var orders = await baseQuery
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            if (orders.Count == 0)
                return new PagedResult<FulfilledOrderDto>(0, request.Page, request.PageSize, new List<FulfilledOrderDto>());

            var orderIds = orders.Select(o => o.OrderId).ToArray();
            var linesByOrder = await _db.OrderLines
                .Where(l => orderIds.Contains(l.OrderId))
                .GroupBy(l => l.OrderId)
                .ToDictionaryAsync(g => g.Key, g => g.ToList(), ct);

            var customerIds = orders.Select(o => o.CustomerId).Distinct().ToArray();
            var productIds  = linesByOrder.SelectMany(kvp => kvp.Value.Select(l => l.ProductId)).Distinct().ToArray();

            var customerMap = new Dictionary<Guid, (string name, string? email)>();
            foreach (var cid in customerIds)
            {
                try
                {
                    var resp = await _customers.GetCustomer(cid, ct);
                    if (resp.IsSuccessStatusCode && resp.Content is not null)
                        customerMap[cid] = (resp.Content.Name, resp.Content.Email);
                }
                catch (ApiException)
                {
                }
            }

            var productMap = new Dictionary<Guid, (string name, string sku)>();
            foreach (var pid in productIds)
            {
                try
                {
                    var resp = await _products.GetProduct(pid, ct);
                    if (resp.IsSuccessStatusCode && resp.Content is not null)
                        productMap[pid] = (resp.Content.Name, resp.Content.Sku);
                }
                catch (ApiException)
                {
                }
            }

            var items = new List<FulfilledOrderDto>(orders.Count);
            foreach (var o in orders)
            {
                var lines = (linesByOrder.TryGetValue(o.OrderId, out var ol)
                                ? ol
                                : new List<Domain.Entities.OrderLine>())
                    .Select(l =>
                    {
                        if (!productMap.TryGetValue(l.ProductId, out var p))
                            p = ("UNKNOWN", "UNKNOWN");

                        return new FulfilledOrderLineDto(
                            ProductId: l.ProductId,
                            ProductName: p.name,
                            Sku: p.sku,
                            Quantity: l.Quantity,
                            UnitPrice: l.UnitPrice,
                            LineTotal: l.Quantity * l.UnitPrice
                        );
                    })
                    .ToList();

                string customerName  = "UNKNOWN";
                string? customerMail = null;
                if (customerMap.TryGetValue(o.CustomerId, out var cust))
                {
                    customerName = cust.name;
                    customerMail = cust.email;
                }

                items.Add(new FulfilledOrderDto(
                    OrderId: o.OrderId,
                    CustomerId: o.CustomerId,
                    CustomerName: customerName,
                    CustomerEmail: customerMail,
                    FulfilledAt: o.FulfilledAt,
                    Total: lines.Sum(x => x.LineTotal),
                    Lines: lines
                ));
            }

            return new PagedResult<FulfilledOrderDto>(total, request.Page, request.PageSize, items);
        }
    }
}
