using MediLink.Order.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediLink.Order.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryProxyController : ControllerBase
{
    private readonly IInventoryService _inventory;

    public InventoryProxyController(IInventoryService inventory)
    {
        _inventory = inventory;
    }

    [HttpGet("medicine/{medicineId:guid}/store/{storeId:guid}")]
    public async Task<IActionResult> GetMedicine(Guid medicineId, Guid storeId)
    {
        var medicine = await _inventory.GetMedicineAsync(medicineId, storeId);
        if (medicine is null)
            return NotFound(new { success = false, message = "Medicine is not stocked by this store." });

        return Ok(new { success = true, item = medicine });
    }

    [HttpGet("stores")]
    public async Task<IActionResult> GetStores()
    {
        var stores = await _inventory.GetStoresAsync();
        return Ok(new { success = true, items = stores });
    }

    [HttpGet("stores/{id:guid}/inventory")]
    public async Task<IActionResult> GetStoreInventory(Guid id)
    {
        var items = await _inventory.GetStoreInventoryAsync(id);
        return Ok(new { success = true, items });
    }
}
