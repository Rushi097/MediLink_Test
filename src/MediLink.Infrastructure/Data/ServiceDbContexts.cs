using MediLink.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Infrastructure.Data;

/// <summary>Database owned exclusively by the Authentication service.</summary>
public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<StoreOwnerProfile> StoreOwnerProfiles => Set<StoreOwnerProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().Ignore(u => u.Cart);
        modelBuilder.Entity<User>().Ignore(u => u.Orders);
        modelBuilder.Entity<StoreOwnerProfile>().Ignore(s => s.Stores);
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>()
            .HasOne(u => u.CustomerProfile).WithOne(c => c.User)
            .HasForeignKey<CustomerProfile>(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<User>()
            .HasOne(u => u.StoreOwnerProfile).WithOne(s => s.User)
            .HasForeignKey<StoreOwnerProfile>(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Database owned exclusively by the Inventory service.</summary>
public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StoreInventory> StoreInventories => Set<StoreInventory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Store>().Ignore(s => s.StoreOwnerProfile);
        modelBuilder.Entity<StoreInventory>()
            .HasOne(si => si.Store).WithMany().HasForeignKey(si => si.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StoreInventory>()
            .HasOne(si => si.Medicine).WithMany().HasForeignKey(si => si.MedicineId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StoreInventory>()
            .HasIndex(si => new { si.StoreId, si.MedicineId }).IsUnique();
        modelBuilder.Entity<StoreInventory>().Property(si => si.Price).HasPrecision(10, 2);
        modelBuilder.Entity<Medicine>().Property(m => m.Price).HasPrecision(10, 2);
    }
}

/// <summary>Database owned exclusively by the Order service. Cross-service IDs are scalar values only.</summary>
public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<StoreOrderAssignment> StoreOrderAssignments => Set<StoreOrderAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().Ignore(o => o.User);
        modelBuilder.Entity<Cart>().Ignore(c => c.User);
        modelBuilder.Entity<CartItem>().Ignore(i => i.Medicine);

        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.Order).WithMany(o => o.Items).HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Cart>()
            .HasMany(c => c.Items).WithOne(i => i.Cart).HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Cart>().HasIndex(c => c.UserId).IsUnique();
        modelBuilder.Entity<StoreOrderAssignment>()
            .HasIndex(a => a.OrderId).IsUnique();
        modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(10, 2);
        modelBuilder.Entity<OrderItem>().Property(i => i.UnitPrice).HasPrecision(10, 2);
    }
}
