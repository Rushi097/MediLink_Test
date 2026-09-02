using MediLink.Order.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediLink.Order.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortalController : ControllerBase
{
    private readonly IInventoryService _inventory;

    public PortalController(IInventoryService inventory)
    {
        _inventory = inventory;
    }

    [HttpGet("stores")]
    public async Task<IActionResult> GetStores()
    {
        var stores = await _inventory.GetStoresAsync();
        return Ok(new { success = true, items = stores });
    }

    [HttpGet("stores/{storeId:guid}/inventory")]
    public async Task<IActionResult> GetStoreInventory(Guid storeId)
    {
        var items = await _inventory.GetStoreInventoryAsync(storeId);
        return Ok(new { success = true, items });
    }

    [HttpGet("admin/overview")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdminMetrics()
    {
        return Ok(new { SystemStatus = "Active", TotalUsers = 1500, TotalStores = 45 });
    }

    [HttpGet("store-owner/inventory")]
    [Authorize(Roles = "StoreOwner,Admin")]
    public IActionResult GetStoreInventory()
    {
        return Ok(new[] { "Medication A - Stock: 200", "Medication B - Stock: 50" });
    }

    [HttpGet("customer/orders")]
    [Authorize(Roles = "Customer,StoreOwner,Admin")]
    public IActionResult GetCustomerOrders()
    {
        return Ok(new[] { "Order #101 - Processing", "Order #102 - Delivered" });
    }
}
