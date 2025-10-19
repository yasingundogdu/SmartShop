using MediatR;
using Customer.Application.Abstractions;

namespace Customer.Application.Customers.Queries;

public sealed class GetCustomerByIdHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto?>
{
    private readonly ICustomerRepository _repo;

    public GetCustomerByIdHandler(ICustomerRepository repo) => _repo = repo;

    public async Task<CustomerDto?> Handle(GetCustomerByIdQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.Id, ct);
        if (entity is null) return null;

        return new CustomerDto(
            entity.CustomerId,
            entity.Name,
            entity.Email
        );
    }
}