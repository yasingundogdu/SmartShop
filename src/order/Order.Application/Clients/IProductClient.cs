using Refit;

public record ProductDto(Guid ProductId, string Sku, string Name);

public interface IProductClient
{
    [Get("/api/products/{id}")]
    Task<ApiResponse<ProductDto>> GetProduct(Guid id, CancellationToken ct = default);
}