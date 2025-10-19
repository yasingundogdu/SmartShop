using Microsoft.EntityFrameworkCore;
using Product.Application.Abstractions;
using Product.Domain.Entities;
using Product.Infrastructure.Persistence;

namespace Product.Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _db;
    public ProductRepository(ProductDbContext db) => _db = db;

    public Task<Domain.Entities.Product?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == id, ct);
}