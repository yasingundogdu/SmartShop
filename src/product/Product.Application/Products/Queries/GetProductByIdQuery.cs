using MediatR;

namespace Product.Application.Products.Queries;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;

public sealed record ProductDto(
    Guid ProductId,
    string Name,
    string Sku,
    decimal Price
);