using Microsoft.EntityFrameworkCore;

namespace Product.Infrastructure.Persistence;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    public DbSet<Product.Domain.Entities.Product> Products => Set<Product.Domain.Entities.Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("product");

        modelBuilder.Entity<Product.Domain.Entities.Product>(b =>
        {
            b.ToTable("product");
            b.HasKey(x => x.ProductId);
            b.Property(x => x.ProductId).HasColumnName("product_id");
            b.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(64).IsRequired();
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.Price).HasColumnName("price").HasColumnType("numeric(18,2)").IsRequired();
            b.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
            b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            b.HasIndex(x => x.Sku).IsUnique();
        });
    }
}