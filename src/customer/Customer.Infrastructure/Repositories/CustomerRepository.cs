using Customer.Application.Abstractions;
using Customer.Domain.Entities;
using Customer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Customer.Infrastructure.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly CustomerDbContext _db;
    public CustomerRepository(CustomerDbContext db) => _db = db;

    public Task<Domain.Entities.Customer?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerId == id, ct);
}