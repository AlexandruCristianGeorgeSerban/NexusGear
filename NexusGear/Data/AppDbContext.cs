using Microsoft.EntityFrameworkCore;
using NexusGear.Models;

namespace NexusGear.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => c.Slug).IsUnique();
        });

        // 2. Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.Property(p => p.RowVersion).IsConcurrencyToken();

            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 3. User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
        });

        // 4. CartItem
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasOne(ci => ci.User)
                  .WithMany(u => u.CartItems)
                  .HasForeignKey(ci => ci.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ci => ci.Product)
                  .WithMany(p => p.CartItems)
                  .HasForeignKey(ci => ci.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 5. WishlistItem
        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.HasOne(wi => wi.User)
                  .WithMany(u => u.WishlistItems)
                  .HasForeignKey(wi => wi.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(wi => wi.Product)
                  .WithMany(p => p.WishlistItems)
                  .HasForeignKey(wi => wi.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 6. Review
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasOne(r => r.User)
                  .WithMany(u => u.Reviews)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Product)
                  .WithMany(p => p.Reviews)
                  .HasForeignKey(r => r.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 7. Address
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasOne(a => a.User)
                  .WithMany(u => u.Addresses)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 8. DiscountCode
        modelBuilder.Entity<DiscountCode>(entity =>
        {
            entity.HasIndex(d => d.Code).IsUnique();
            entity.Property(d => d.DiscountValue).HasPrecision(18, 2);
        });

        // 9. Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.OrderNumber).IsUnique();
            entity.Property(o => o.ShippingCost).HasPrecision(18, 2);
            entity.Property(o => o.DiscountAmount).HasPrecision(18, 2);
            entity.Property(o => o.TotalAmount).HasPrecision(18, 2);

            entity.HasOne(o => o.User)
                  .WithMany(u => u.Orders)
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Address)
                  .WithMany(a => a.Orders)
                  .HasForeignKey(o => o.AddressId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.DiscountCode)
                  .WithMany(d => d.Orders)
                  .HasForeignKey(o => o.DiscountCodeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 10. OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(oi => oi.UnitPrice).HasPrecision(18, 2);

            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.OrderItems)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.Product)
                  .WithMany(p => p.OrderItems)
                  .HasForeignKey(oi => oi.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

