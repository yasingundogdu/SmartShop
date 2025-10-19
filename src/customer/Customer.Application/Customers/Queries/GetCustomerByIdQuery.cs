using MediatR;

namespace Customer.Application.Customers.Queries;

public sealed record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto?>;

public sealed record CustomerDto(
    Guid CustomerId,
    string Name,
    string? Email
);