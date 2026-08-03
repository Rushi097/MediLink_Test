using Microsoft.EntityFrameworkCore;
using MediLink.Core.Entities;

namespace MediLink.Infrastructure.Data;

public class MediLinkDbContext : DbContext
{
    public MediLinkDbContext(DbContextOptions<MediLinkDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<StoreOwnerProfile> StoreOwnerProfiles => Set<StoreOwnerProfile>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StoreInventory> StoreInventories => Set<StoreInventory>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // One-to-One: User -> CustomerProfile
        modelBuilder.Entity<User>()
            .HasOne(u => u.CustomerProfile)
            .WithOne(c => c.User)
            .HasForeignKey<CustomerProfile>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-One: User -> StoreOwnerProfile
        modelBuilder.Entity<User>()
            .HasOne(u => u.StoreOwnerProfile)
            .WithOne(s => s.User)
            .HasForeignKey<StoreOwnerProfile>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-Many: StoreOwnerProfile -> Stores
        modelBuilder.Entity<StoreOwnerProfile>()
            .HasMany(s => s.Stores)
            .WithOne(st => st.StoreOwnerProfile)
            .HasForeignKey(st => st.StoreOwnerProfileId);

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>().HasOne(u => u.Cart).WithOne(c => c.User)
            .HasForeignKey<Cart>(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CartItem>().HasOne(i => i.Cart).WithMany(c => c.Items)
            .HasForeignKey(i => i.CartId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CartItem>().HasOne(i => i.Medicine).WithMany()
            .HasForeignKey(i => i.MedicineId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>().HasOne(o => o.User).WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OrderItem>().HasOne(i => i.Order).WithMany(o => o.Items)
            .HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StoreInventory>()
            .HasOne(si => si.Store)
            .WithMany()
            .HasForeignKey(si => si.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StoreInventory>()
            .HasOne(si => si.Medicine)
            .WithMany()
            .HasForeignKey(si => si.MedicineId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StoreInventory>()
            .HasIndex(si => new { si.StoreId, si.MedicineId })
            .IsUnique();
        modelBuilder.Entity<Medicine>().Property(m => m.Price).HasPrecision(10, 2);
        modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(10, 2);
        modelBuilder.Entity<OrderItem>().Property(i => i.UnitPrice).HasPrecision(10, 2);
    }
}
