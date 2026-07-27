using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortalController : ControllerBase
{
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