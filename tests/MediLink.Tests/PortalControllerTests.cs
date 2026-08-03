using MediLink.Api.Controllers;
using MediLink.Core.Entities;
using MediLink.Core.Enums;
using MediLink.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Tests;

public class PortalControllerTests
{
    [Fact]
    public async Task GetStores_returns_registered_store_list()
    {
        var options = new DbContextOptionsBuilder<MediLinkDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new MediLinkDbContext(options);
        var user = new User
        {
            Email = "store.owner@example.com",
            PasswordHash = "hash",
            FirstName = "Amit",
            LastName = "Sharma",
            Role = UserRole.StoreOwner
        };

        var profile = new StoreOwnerProfile
        {
            UserId = user.Id,
            User = user,
            BusinessLicenseNumber = "LIC-2893"
        };

        var store = new Store
        {
            Name = "City Care Pharmacy",
            Address = "Pune, Maharashtra",
            StoreOwnerProfileId = profile.Id,
            StoreOwnerProfile = profile
        };

        db.Users.Add(user);
        db.StoreOwnerProfiles.Add(profile);
        db.Stores.Add(store);
        await db.SaveChangesAsync();

        var controller = new PortalController(db);

        var result = await controller.GetStores();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = ok.Value as dynamic;
        Assert.NotNull(payload);
    }
}
