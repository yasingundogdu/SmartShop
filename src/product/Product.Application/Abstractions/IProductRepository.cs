using Product.Domain.Entities;

namespace Product.Application.Abstractions;

public interface IProductRepository
{
    Task<Domain.Entities.Product?> GetByIdAsync(Guid id, CancellationToken ct);
}