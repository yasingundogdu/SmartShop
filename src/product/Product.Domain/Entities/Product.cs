namespace Product.Domain.Entities;

public enum ProductStatus { Active = 1, Inactive = 2 }

public class Product
{
    public Guid ProductId { get; private set; } = Guid.NewGuid();
    public string Sku { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Active;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    public static Product Create(string sku, string name, decimal price, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(sku)) 
            throw new ArgumentException("SKU required");
        
        if (string.IsNullOrWhiteSpace(name)) 
            throw new ArgumentException("Name required");
        
        if (price < 0) 
            throw new ArgumentOutOfRangeException(nameof(price));
        
        return new Product
        {
            Sku = sku.Trim(), 
            Name = name.Trim(), 
            Price = price, 
            Description = description
        };
    }

    public void Update(string name, decimal price, string? description, ProductStatus status)
    {
        if (string.IsNullOrWhiteSpace(name)) 
            throw new ArgumentException("Name required");
        
        if (price < 0) 
            throw new ArgumentOutOfRangeException(nameof(price));
        
        Name = name.Trim();
        Price = price;
        Description = description;
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }
}