using System.Net.Http.Json;
using System.Security.Claims;
using MediLink.Infrastructure.Data;
using MediLink.Order.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Order.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IInventoryService _inventory;
    private readonly IHttpClientFactory _httpClientFactory;

    public DashboardController(OrderDbContext db, IInventoryService inventory, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _inventory = inventory;
        _httpClientFactory = httpClientFactory;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("customer")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Customer()
    {
        var orders = await _db.Orders.Where(o => o.UserId == UserId).OrderByDescending(o => o.CreatedAt).Take(5).ToListAsync();
        return Ok(new
        {
            success = true,
            orderCount = await _db.Orders.CountAsync(o => o.UserId == UserId),
            activeOrders = orders.Count(o => o.Status is MediLink.Core.Entities.OrderStatus.PendingStoreAcceptance or MediLink.Core.Entities.OrderStatus.Accepted or MediLink.Core.Entities.OrderStatus.Preparing or MediLink.Core.Entities.OrderStatus.ReadyForDelivery or MediLink.Core.Entities.OrderStatus.OutForDelivery),
            recentOrders = orders
        });
    }

    [HttpGet("store")]
    [Authorize(Roles = "StoreOwner")]
    public async Task<IActionResult> Store()
    {
        var summary = await _inventory.GetInventorySummaryAsync();
        return Ok(new
        {
            success = true,
            activeProducts = summary.ActiveProducts,
            lowStock = summary.LowStock,
            inventoryValue = summary.InventoryValue,
            lowStockItems = summary.LowStockItems
        });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Admin()
    {
        var orders = await _db.Orders.ToListAsync();
        var inventorySummary = await _inventory.GetInventorySummaryAsync();
        var authClient = _httpClientFactory.CreateClient("auth");
        authClient.DefaultRequestHeaders.TryAddWithoutValidation("X-MediLink-Internal-Key", Environment.GetEnvironmentVariable("MEDILINK_INTERNAL_KEY") ?? "medilink-internal-development-key");
        var authStats = await authClient.GetFromJsonAsync<AuthStatsDto>("api/auth/internal/stats") ?? new AuthStatsDto();

        return Ok(new
        {
            success = true,
            customers = authStats.Customers,
            pharmacyOwners = authStats.PharmacyOwners,
            medicines = inventorySummary.ActiveProducts,
            orders = orders.Count,
            deliveredRevenue = orders.Where(o => o.Status == MediLink.Core.Entities.OrderStatus.Delivered).Sum(o => o.TotalAmount),
            pendingOrders = orders.Count(o => o.Status == MediLink.Core.Entities.OrderStatus.PendingStoreAcceptance),
            activeProducts = inventorySummary.ActiveProducts,
            lowStock = inventorySummary.LowStock,
            inventoryValue = inventorySummary.InventoryValue
        });
    }
}

public sealed record AuthStatsDto
{
    public int Customers { get; init; }
    public int PharmacyOwners { get; init; }
}
