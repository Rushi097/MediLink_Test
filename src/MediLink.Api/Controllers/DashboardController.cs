using System.Security.Claims;
using MediLink.Core.Enums;
using MediLink.Core.Entities;
using MediLink.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Api.Controllers;

[ApiController, Route("api/dashboard"), Authorize]
public class DashboardController(MediLinkDbContext db) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("customer")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Customer()
    {
        var orders = await db.Orders.Where(o => o.UserId == UserId).OrderByDescending(o => o.CreatedAt).Take(5).ToListAsync();
        return Ok(new { success = true, orderCount = await db.Orders.CountAsync(o => o.UserId == UserId), activeOrders = orders.Count(o => o.Status is OrderStatus.Placed or OrderStatus.Confirmed or OrderStatus.Shipped), recentOrders = orders });
    }

    [HttpGet("store")]
    [Authorize(Roles = "StoreOwner")]
    public async Task<IActionResult> Store()
    {
        var medicines = await db.Medicines.Where(m => m.IsActive).ToListAsync();
        return Ok(new { success = true, activeProducts = medicines.Count, lowStock = medicines.Count(m => m.StockQuantity <= 10), inventoryValue = medicines.Sum(m => m.Price * m.StockQuantity), lowStockItems = medicines.Where(m => m.StockQuantity <= 10).OrderBy(m => m.StockQuantity).Take(8) });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Admin()
    {
        var orders = await db.Orders.ToListAsync();
        return Ok(new
        {
            success = true,
            customers = await db.Users.CountAsync(u => u.Role == UserRole.Customer),
            pharmacyOwners = await db.Users.CountAsync(u => u.Role == UserRole.StoreOwner),
            medicines = await db.Medicines.CountAsync(m => m.IsActive),
            orders = orders.Count,
            deliveredRevenue = orders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount),
            pendingOrders = orders.Count(o => o.Status == OrderStatus.Placed)
        });
    }
}
