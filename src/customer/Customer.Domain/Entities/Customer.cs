namespace Customer.Domain.Entities;

public enum CustomerStatus { Active = 1, Inactive = 2 }

public class Customer
{
    public Guid CustomerId { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public CustomerStatus Status { get; private set; } = CustomerStatus.Active;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    public static Customer Create(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name)) 
            throw new ArgumentException("Name required");
        
        if (string.IsNullOrWhiteSpace(email)) 
            throw new ArgumentException("Email required");
        
        return new Customer
        {
            Name = name.Trim(), 
            Email = email.Trim().ToLowerInvariant()
        };
    }

    public void Update(string name, string email, CustomerStatus status)
    {
        if (string.IsNullOrWhiteSpace(name)) 
            throw new ArgumentException("Name required");
        
        if (string.IsNullOrWhiteSpace(email)) 
            throw new ArgumentException("Email required");
        
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }
}