using MediLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Tests;

public class DatabaseBoundaryTests
{
    [Fact]
    public void Auth_model_contains_only_auth_entities()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new AuthDbContext(options);
        var names = db.Model.GetEntityTypes().Select(x => x.ClrType.Name).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "CustomerProfile", "StoreOwnerProfile", "User" }, names);
    }

    [Fact]
    public void Inventory_model_contains_only_inventory_entities()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new InventoryDbContext(options);
        var names = db.Model.GetEntityTypes().Select(x => x.ClrType.Name).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "Medicine", "Store", "StoreInventory" }, names);
    }

    [Fact]
    public void Order_model_contains_only_order_entities()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new OrderDbContext(options);
        var names = db.Model.GetEntityTypes().Select(x => x.ClrType.Name).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "Cart", "CartItem", "Order", "OrderItem", "StoreOrderAssignment" }, names);
    }
}
