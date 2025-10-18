using Refit;

public record CustomerDto(Guid CustomerId, string Name, string Email);

public interface ICustomerClient
{
    [Get("/api/customers/{id}")]
    Task<ApiResponse<CustomerDto>> GetCustomer(Guid id, CancellationToken ct = default);
}