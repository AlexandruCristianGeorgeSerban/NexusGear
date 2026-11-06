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
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
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
            entity.ToTable("IN_CART");
            entity.HasIndex(ci => new { ci.UserId, ci.ProductId }).IsUnique();

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
            entity.ToTable("IN_WISHLIST");
            entity.HasIndex(wi => new { wi.UserId, wi.ProductId }).IsUnique();

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
            entity.ToTable("REVIEWS");

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
            // Address stands on its own; linked to Users via UserAddress (HAS_ADDRESS)
            // and to Orders via Order.AddressId (SHIPS_TO)
        });

        // 8. UserAddress
        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.ToTable("HAS_ADDRESS");
            entity.HasIndex(ua => new { ua.UserId, ua.AddressId }).IsUnique();

            entity.HasOne(ua => ua.User)
                  .WithMany(u => u.UserAddresses)
                  .HasForeignKey(ua => ua.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ua => ua.Address)
                  .WithMany(a => a.UserAddresses)
                  .HasForeignKey(ua => ua.AddressId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 9. DiscountCode
        modelBuilder.Entity<DiscountCode>(entity =>
        {
            entity.HasIndex(d => d.Code).IsUnique();
            entity.Property(d => d.DiscountValue).HasPrecision(18, 2);
        });

        // 10. Order
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

        // 11. OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("CONTAINS");
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
