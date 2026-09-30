using System.Security.Claims;
using MediLink.Core.DTOs;
using MediLink.Core.Entities;
using OrderEntity = MediLink.Core.Entities.Order;
using MediLink.Infrastructure.Data;
using MediLink.Order.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Order.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IInventoryService _inventory;

    public OrdersController(OrderDbContext db, IInventoryService inventory)
    {
        _db = db;
        _inventory = inventory;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _db.Orders.Where(o => o.UserId == UserId)
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var orderIds = orders.Select(o => o.Id).ToList();
        var assignments = await _db.StoreOrderAssignments.AsNoTracking()
            .Where(a => orderIds.Contains(a.OrderId))
            .ToListAsync();

        var legacyStoreIds = assignments
            .Where(a => string.IsNullOrWhiteSpace(a.StoreName))
            .Select(a => a.StoreId)
            .Distinct()
            .Where(id => id != Guid.Empty)
            .ToList();

        var stores = legacyStoreIds.Count == 0
            ? Array.Empty<InventoryStoreDto>()
            : (await _inventory.GetStoresAsync()).Where(s => legacyStoreIds.Contains(s.Id)).ToArray();
        var storeById = stores.ToDictionary(s => s.Id);

        var items = orders.Select(o =>
        {
            var assignment = assignments.FirstOrDefault(a => a.OrderId == o.Id);
            InventoryStoreDto? store = null;
            if (assignment is not null)
            {
                store = !string.IsNullOrWhiteSpace(assignment.StoreName)
                    ? new InventoryStoreDto { Id = assignment.StoreId, Name = assignment.StoreName, Address = assignment.StoreAddress }
                    : (storeById.TryGetValue(assignment.StoreId, out var legacy) ? legacy : null);
            }
            return ToView(o, store);
        }).ToList();

        return Ok(new { success = true, items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == UserId);
        if (order is null)
            return NotFound(new { success = false, message = "Order was not found." });

        var assignment = await _db.StoreOrderAssignments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.OrderId == order.Id);
        InventoryStoreDto? store = null;
        if (assignment is not null)
        {
            if (!string.IsNullOrWhiteSpace(assignment.StoreName))
                store = new InventoryStoreDto { Id = assignment.StoreId, Name = assignment.StoreName, Address = assignment.StoreAddress };
            else
                store = (await _inventory.GetStoresAsync()).FirstOrDefault(s => s.Id == assignment.StoreId);
        }

        return Ok(new { success = true, item = ToView(order, store) });
    }

    [HttpPost]
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(CheckoutRequest request)
    {
        var address = request.DeliveryAddress?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(address))
            return BadRequest(new { success = false, message = "Delivery address is required." });
        if (address.Length < 10)
            return BadRequest(new { success = false, message = "Please enter a complete delivery address." });
        if (!string.Equals(request.PaymentMethod, "CashOnDelivery", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { success = false, message = "Only Cash on Delivery is enabled for this project." });

        var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == UserId);
        if (cart is null || cart.Items.Count == 0)
            return BadRequest(new { success = false, message = "Your cart is empty." });

        var storeIds = cart.Items.Select(i => i.StoreId).Distinct().ToList();
        if (storeIds.Count != 1 || storeIds[0] == Guid.Empty)
            return BadRequest(new { success = false, message = "A checkout can contain medicines from one medical store only." });
        if (request.StoreId == Guid.Empty || request.StoreId != storeIds[0])
            return BadRequest(new { success = false, message = "The selected medical store does not match your cart." });
        var storeId = request.StoreId;
        var selectedStore = (await _inventory.GetStoresAsync()).FirstOrDefault(s => s.Id == storeId);
        if (selectedStore is null)
            return BadRequest(new { success = false, message = "The selected medical store is no longer available." });

        var inventoryItems = new List<InventoryMedicineDto>();
        foreach (var item in cart.Items)
        {
            var medicine = await _inventory.GetMedicineAsync(item.MedicineId, item.StoreId);
            if (medicine is null || !medicine.IsActive)
                return BadRequest(new { success = false, message = $"{item.MedicineName} is no longer available." });
            if (medicine.StockQuantity < item.Quantity)
                return BadRequest(new { success = false, message = $"Not enough stock for {medicine.Name}." });
            inventoryItems.Add(medicine);
        }

        var reservationId = Guid.NewGuid();
        var order = new OrderEntity
        {
            UserId = UserId,
            DeliveryAddress = address,
            PaymentMethod = "CashOnDelivery",
            ReservationId = reservationId,
            TotalAmount = cart.Items.Zip(inventoryItems, (cartItem, medicine) => cartItem.Quantity * medicine.Price).Sum(),
            Status = OrderStatus.PendingStoreAcceptance,
            Items = cart.Items.Zip(inventoryItems, (cartItem, medicine) => new OrderItem
            {
                MedicineId = cartItem.MedicineId,
                StoreId = cartItem.StoreId,
                MedicineName = medicine.Name,
                UnitPrice = medicine.Price,
                Quantity = cartItem.Quantity
            }).ToList()
        };

        var reservedItems = new List<(Guid MedicineId, Guid StoreId, int Quantity)>();
        foreach (var item in cart.Items)
        {
            var success = await _inventory.ReserveStockAsync(item.MedicineId, item.StoreId, item.Quantity, reservationId);
            if (!success)
            {
                foreach (var reserved in reservedItems)
                    await _inventory.RestockAsync(reserved.MedicineId, reserved.StoreId, reserved.Quantity, reservationId);
                return BadRequest(new { success = false, message = "Could not reserve inventory for one or more items." });
            }
            reservedItems.Add((item.MedicineId, item.StoreId, item.Quantity));
        }

        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            _db.Orders.Add(order);
            _db.StoreOrderAssignments.Add(new StoreOrderAssignment
            {
                StoreId = storeId,
                OrderId = order.Id,
                StoreName = selectedStore.Name,
                StoreAddress = selectedStore.Address
            });
            _db.CartItems.RemoveRange(cart.Items);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            foreach (var reserved in reservedItems)
                await _inventory.RestockAsync(reserved.MedicineId, reserved.StoreId, reserved.Quantity, reservationId);
            return StatusCode(500, new { success = false, message = "The order could not be created. No inventory was left reserved." });
        }

        return CreatedAtAction(nameof(Get), new { id = order.Id }, new { success = true, item = ToView(order, selectedStore) });
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var order = await _db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == UserId);
        if (order is null) return NotFound(new { success = false, message = "Order was not found." });
        if (order.Status is not (OrderStatus.PendingStoreAcceptance or OrderStatus.Accepted))
            return BadRequest(new { success = false, message = "This order can no longer be cancelled." });

        foreach (var item in order.Items)
        {
            if (!await _inventory.RestockAsync(item.MedicineId, item.StoreId, item.Quantity, order.ReservationId))
                return BadRequest(new { success = false, message = "Could not restore inventory for one or more items." });
        }

        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync();
        var assignment = await _db.StoreOrderAssignments.AsNoTracking().FirstOrDefaultAsync(a => a.OrderId == order.Id);
        var selectedStore = assignment is null ? null : new InventoryStoreDto
        {
            Id = assignment.StoreId, Name = assignment.StoreName, Address = assignment.StoreAddress
        };
        return Ok(new { success = true, item = ToView(order, selectedStore) });
    }

    private static object ToView(OrderEntity order, InventoryStoreDto? store) => new
    {
        order.Id, order.UserId, order.DeliveryAddress, order.TotalAmount, order.PaymentMethod, order.Status, order.CreatedAt,
        StoreId = order.Items.Select(i => i.StoreId).FirstOrDefault(),
        Store = store is null ? null : new { store.Id, store.Name, store.Address },
        Items = order.Items.Select(i => new { i.Id, i.MedicineId, i.StoreId, i.MedicineName, i.UnitPrice, i.Quantity })
    };
}
