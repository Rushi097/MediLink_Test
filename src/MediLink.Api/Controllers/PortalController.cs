using MediLink.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortalController(MediLinkDbContext db) : ControllerBase
{
    [HttpGet("stores")]
    public async Task<IActionResult> GetStores()
    {
        var stores = await db.Stores
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new
            {
                id = s.Id,
                name = s.Name,
                address = s.Address,
                ownerName = s.StoreOwnerProfile.User.FirstName + " " + s.StoreOwnerProfile.User.LastName
            })
            .ToListAsync();

        return Ok(new { success = true, items = stores });
    }

    [HttpGet("stores/{storeId:guid}/inventory")]
    public async Task<IActionResult> GetStoreInventory(Guid storeId)
    {
        var storeExists = await db.Stores.AnyAsync(s => s.Id == storeId);
        if (!storeExists)
            return NotFound(new { success = false, message = "Medical store not found." });

        var items = await db.Medicines
            .AsNoTracking()
            .Where(m => m.IsActive && db.StoreInventories.Any(si => si.StoreId == storeId && si.MedicineId == m.Id))
            .OrderBy(m => m.Name)
            .Select(m => new
            {
                id = m.Id,
                name = m.Name,
                category = m.Category,
                description = m.Description,
                price = m.Price,
                stockQuantity = m.StockQuantity,
                imageUrl = m.ImageUrl
            })
            .ToListAsync();

        return Ok(new { success = true, items });
    }

    // Accessible ONLY by Admins
    [HttpGet("admin/overview")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdminMetrics()
    {
        return Ok(new { SystemStatus = "Active", TotalUsers = 1500, TotalStores = 45 });
    }

    // Accessible by Store Owners & Admins
    [HttpGet("store-owner/inventory")]
    [Authorize(Roles = "StoreOwner,Admin")]
    public IActionResult GetStoreInventory()
    {
        return Ok(new[] { "Medication A - Stock: 200", "Medication B - Stock: 50" });
    }

    // Accessible by Customers, Store Owners & Admins
    [HttpGet("customer/orders")]
    [Authorize(Roles = "Customer,StoreOwner,Admin")]
    public IActionResult GetCustomerOrders()
    {
        return Ok(new[] { "Order #101 - Processing", "Order #102 - Delivered" });
    }
}