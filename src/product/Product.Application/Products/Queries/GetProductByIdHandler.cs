using MediatR;
using Product.Application.Abstractions;

namespace Product.Application.Products.Queries;

public sealed class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _repo;

    public GetProductByIdHandler(IProductRepository repo) => _repo = repo;

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.Id, ct);
        if (entity is null) return null;

        return new ProductDto(
            entity.ProductId,
            entity.Name,
            entity.Sku,
            entity.Price
        );
    }
}