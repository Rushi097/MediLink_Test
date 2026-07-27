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
    }
}