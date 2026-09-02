using System.Net.Http.Json;
using MediLink.Core.Entities;
using MediLink.Infrastructure.Data;
using MediLink.Order.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Order.Controllers;

[ApiController]
[Route("api/store-orders")]
[Authorize(Roles = "StoreOwner,Admin")]
public class StoreOrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IInventoryService _inventory;
    private readonly string _internalKey;

    public StoreOrdersController(OrderDbContext db, IHttpClientFactory httpClientFactory, IInventoryService inventory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _inventory = inventory;
        _internalKey = Environment.GetEnvironmentVariable("MEDILINK_INTERNAL_KEY") ?? "medilink-internal-development-key";
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var storeId = await ResolveStoreIdAsync();
        if (storeId is null) return Forbid();

        var assignments = await _db.StoreOrderAssignments.AsNoTracking().ToListAsync();
        var ordersQuery = _db.Orders.AsNoTracking().Include(o => o.Items).OrderByDescending(o => o.CreatedAt);
        var orders = await ordersQuery.ToListAsync();
        if (!User.IsInRole("Admin"))
            orders = orders.Where(o => assignments.Any(a => a.OrderId == o.Id && a.StoreId == storeId.Value)).ToList();

        var authClient = _httpClientFactory.CreateClient("auth");
        var result = new List<object>();
        foreach (var order in orders)
        {
            var assignment = assignments.FirstOrDefault(a => a.OrderId == order.Id);
            var customer = await GetUserAsync(authClient, order.UserId);
            result.Add(new
            {
                id = order.Id, userId = order.UserId, deliveryAddress = order.DeliveryAddress,
                totalAmount = order.TotalAmount, status = order.Status, createdAt = order.CreatedAt,
                claimed = assignment is not null, claimedByStoreId = assignment?.StoreId,
                customer = customer?.User, customerProfile = customer?.CustomerProfile,
                items = order.Items.Select(i => new { i.Id, i.MedicineId, i.StoreId, i.MedicineName, i.UnitPrice, i.Quantity })
            });
        }
        return Ok(new { success = true, items = result });
    }

    // Kept for backwards compatibility with older store-portal builds.
    [HttpPost("{orderId:guid}/claim")]
    public async Task<IActionResult> Claim(Guid orderId)
    {
        var storeId = await ResolveStoreIdAsync();
        if (storeId is null || User.IsInRole("Admin")) return Forbid();
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return NotFound(new { success = false, message = "Order not found." });
        if (order.Status != OrderStatus.PendingStoreAcceptance)
            return BadRequest(new { success = false, message = "Only pending orders can be claimed." });
        var existing = await _db.StoreOrderAssignments.FirstOrDefaultAsync(a => a.OrderId == orderId);
        if (existing is not null && existing.StoreId != storeId.Value)
            return Conflict(new { success = false, message = "This order belongs to another store." });
        if (existing is null)
        {
            _db.StoreOrderAssignments.Add(new StoreOrderAssignment { StoreId = storeId.Value, OrderId = orderId });
            await _db.SaveChangesAsync();
        }
        return Ok(new { success = true });
    }

    [HttpPost("{orderId:guid}/accept")]
    public Task<IActionResult> Accept(Guid orderId) => ChangeAcceptance(orderId, true);

    [HttpPost("{orderId:guid}/reject")]
    public Task<IActionResult> Reject(Guid orderId) => ChangeAcceptance(orderId, false);

    private async Task<IActionResult> ChangeAcceptance(Guid orderId, bool accept)
    {
        var storeId = await ResolveStoreIdAsync();
        if (storeId is null || User.IsInRole("Admin")) return Forbid();
        var assignment = await _db.StoreOrderAssignments.FirstOrDefaultAsync(a => a.OrderId == orderId && a.StoreId == storeId.Value);
        if (assignment is null) return Forbid();
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return NotFound(new { success = false, message = "Order not found." });
        if (order.Status != OrderStatus.PendingStoreAcceptance)
            return BadRequest(new { success = false, message = "This order has already been processed." });

        if (accept)
        {
            order.Status = OrderStatus.Accepted;
            await _db.SaveChangesAsync();
            return Ok(new { success = true, item = new { order.Id, order.Status } });
        }

        var restored = new List<(Guid MedicineId, Guid StoreId, int Quantity)>();
        foreach (var item in order.Items)
        {
            if (!await _inventory.RestockAsync(item.MedicineId, item.StoreId, item.Quantity, order.ReservationId))
            {
                foreach (var r in restored)
                    await _inventory.ReserveStockAsync(r.MedicineId, r.StoreId, r.Quantity, order.ReservationId);
                return BadRequest(new { success = false, message = "Could not restore inventory for one or more items. The order remains pending." });
            }
            restored.Add((item.MedicineId, item.StoreId, item.Quantity));
        }

        order.Status = OrderStatus.Rejected;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, item = new { order.Id, order.Status } });
    }

    [HttpPut("{orderId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid orderId, [FromBody] StoreOrderStatusRequest request)
    {
        var storeId = await ResolveStoreIdAsync();
        if (storeId is null || User.IsInRole("Admin")) return Forbid();
        var assignment = await _db.StoreOrderAssignments.FirstOrDefaultAsync(a => a.OrderId == orderId && a.StoreId == storeId.Value);
        if (assignment is null) return Forbid();
        var order = await _db.Orders.FindAsync(orderId);
        if (order is null) return NotFound(new { success = false, message = "Order not found." });
        if (order.Status == OrderStatus.Rejected || order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Delivered)
            return BadRequest(new { success = false, message = "This order cannot be updated further." });
        if (request.Status is not (OrderStatus.Preparing or OrderStatus.ReadyForDelivery or OrderStatus.OutForDelivery or OrderStatus.Delivered))
            return BadRequest(new { success = false, message = "Store owners can set Preparing, ReadyForDelivery, OutForDelivery or Delivered." });

        var validNext = order.Status switch
        {
            OrderStatus.Accepted => request.Status == OrderStatus.Preparing,
            OrderStatus.Preparing => request.Status == OrderStatus.ReadyForDelivery,
            OrderStatus.ReadyForDelivery => request.Status == OrderStatus.OutForDelivery,
            OrderStatus.OutForDelivery => request.Status == OrderStatus.Delivered,
            _ => false
        };
        if (!validNext)
            return BadRequest(new { success = false, message = $"Invalid status transition from {order.Status} to {request.Status}." });

        order.Status = request.Status;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, item = new { order.Id, order.Status } });
    }

    private async Task<Guid?> ResolveStoreIdAsync()
    {
        if (User.IsInRole("Admin")) return Guid.Empty;
        var profile = User.FindFirst("StoreOwnerProfileId")?.Value;
        if (!Guid.TryParse(profile, out var profileId)) return null;
        var client = _httpClientFactory.CreateClient("inventory");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/stores/internal/owner/{profileId}");
        request.Headers.TryAddWithoutValidation("X-MediLink-Internal-Key", _internalKey);
        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        var payload = await response.Content.ReadFromJsonAsync<StoreResponse>();
        return payload?.Item?.Id;
    }

    private async Task<UserResponse?> GetUserAsync(HttpClient client, Guid userId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/auth/internal/users/{userId}");
        request.Headers.TryAddWithoutValidation("X-MediLink-Internal-Key", _internalKey);
        using var response = await client.SendAsync(request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<UserResponse>() : null;
    }

    private sealed record StoreResponse(bool Success, StoreDto? Item);
    private sealed record StoreDto(Guid Id, string Name, string Address, Guid StoreOwnerProfileId);
    private sealed record UserResponse(bool Success, UserDto User, CustomerProfileDto? CustomerProfile, StoreOwnerProfileDto? StoreOwnerProfile);
    private sealed record UserDto(Guid Id, string Email, string FirstName, string LastName, string Role);
    private sealed record CustomerProfileDto(string PhoneNumber, string DeliveryAddress);
    private sealed record StoreOwnerProfileDto(Guid Id, string BusinessLicenseNumber);
}

public sealed class StoreOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}
