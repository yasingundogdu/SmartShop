using Customer.Domain.Entities;

namespace Customer.Application.Abstractions;

public interface ICustomerRepository
{
    Task<Domain.Entities.Customer?> GetByIdAsync(Guid id, CancellationToken ct);
}