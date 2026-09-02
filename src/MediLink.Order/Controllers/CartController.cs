using System.Security.Claims;
using MediLink.Infrastructure.Data;
using MediLink.Order.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Order.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IInventoryService _inventory;

    public CartController(OrderDbContext db, IInventoryService inventory)
    {
        _db = db;
        _inventory = inventory;
    }

    private bool IsCustomerUser() => User.Claims.Any(c =>
        (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "Role") &&
        string.Equals(c.Value, "Customer", StringComparison.OrdinalIgnoreCase));

    private bool TryGetUserId(out Guid userId)
    {
        var values = new[]
        {
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.FindFirstValue("sub"),
            User.FindFirstValue("userId"),
            User.FindFirstValue("UserId")
        };
        foreach (var value in values)
            if (Guid.TryParse(value, out userId)) return true;
        userId = Guid.Empty;
        return false;
    }

    private IActionResult CustomerOnly() =>
        Unauthorized(new { success = false, code = "CUSTOMER_SESSION_REQUIRED", message = "Customer authentication is required. Please sign in again." });

    private Guid UserId => TryGetUserId(out var id) ? id : Guid.Empty;

    private Task<MediLink.Core.Entities.Cart?> CartQuery() => _db.Carts
        .Include(c => c.Items)
        .FirstOrDefaultAsync(c => c.UserId == UserId);

    private static object View(MediLink.Core.Entities.Cart cart) => new
    {
        cart.Id,
        items = cart.Items.Select(i => new
        {
            i.Id,
            i.MedicineId,
            i.StoreId,
            name = i.MedicineName,
            price = i.UnitPrice,
            imageUrl = i.ImageUrl,
            i.Quantity,
            subtotal = i.Quantity * i.UnitPrice
        }),
        total = cart.Items.Sum(i => i.Quantity * i.UnitPrice)
    };

    // Self-heals carts created by older MediLink versions. This is deliberately
    // conservative: it removes only rows that can no longer be routed safely.
    private async Task<MediLink.Core.Entities.Cart> GetOrRepairCartAsync()
    {
        var cart = await CartQuery();
        if (cart is null)
        {
            cart = new MediLink.Core.Entities.Cart { UserId = UserId };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
            return cart;
        }

        var invalid = new List<MediLink.Core.Entities.CartItem>();
        foreach (var item in cart.Items)
        {
            if (item.StoreId == Guid.Empty || item.MedicineId == Guid.Empty || item.Quantity <= 0)
            {
                invalid.Add(item);
                continue;
            }

            // Older rows may point at a medicine/store offer that no longer
            // exists. Remove those rows so they cannot block a new cart.
            try
            {
                var offer = await _inventory.GetMedicineAsync(item.MedicineId, item.StoreId);
                if (offer is null || !offer.IsActive)
                    invalid.Add(item);
            }
            catch
            {
                // Do not destroy a customer's cart because Inventory is
                // temporarily unavailable. Keep the row and let Add/Checkout
                // report the live inventory problem explicitly.
            }
        }

        if (invalid.Count > 0)
        {
            _db.CartItems.RemoveRange(invalid);
            await _db.SaveChangesAsync();
            cart = await CartQuery() ?? cart;
        }

        return cart;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!IsCustomerUser() || UserId == Guid.Empty) return CustomerOnly();
        var cart = await GetOrRepairCartAsync();
        return Ok(new { success = true, cart = View(cart) });
    }

    [HttpPost("repair")]
    public async Task<IActionResult> Repair()
    {
        if (!IsCustomerUser() || UserId == Guid.Empty) return CustomerOnly();
        var cart = await GetOrRepairCartAsync();
        return Ok(new { success = true, repaired = true, cart = View(cart) });
    }

    [HttpPost("items")]
    public async Task<IActionResult> Add(MediLink.Core.DTOs.CartItemRequest request)
    {
        if (!IsCustomerUser() || UserId == Guid.Empty) return CustomerOnly();
        if (request.MedicineId == Guid.Empty || request.StoreId == Guid.Empty || request.Quantity <= 0)
            return BadRequest(new { success = false, code = "INVALID_CART_REQUEST", message = "A valid medicine, medical store and quantity are required." });

        var medicine = await _inventory.GetMedicineAsync(request.MedicineId, request.StoreId);
        if (medicine is null && !string.IsNullOrWhiteSpace(request.ExternalMedicineId))
            medicine = await _inventory.GetMedicineByExternalIdAsync(request.ExternalMedicineId, request.StoreId);
        if (medicine is null || !medicine.IsActive)
            return NotFound(new { success = false, code = "STORE_OFFER_NOT_FOUND", message = "This medicine is not currently offered by the selected medical store." });

        var cart = await GetOrRepairCartAsync();

        // One store per checkout. Existing valid items remain untouched.
        if (cart.Items.Count > 0 && cart.Items.Any(i => i.StoreId != request.StoreId))
            return Conflict(new { success = false, code = "DIFFERENT_STORE_CART", message = "Your cart belongs to another medical store. Empty the current cart before choosing a different store." });

        var item = cart.Items.FirstOrDefault(i => i.MedicineId == medicine.Id && i.StoreId == request.StoreId);
        var newQuantity = request.Quantity + (item?.Quantity ?? 0);
        if (medicine.StockQuantity < newQuantity)
            return BadRequest(new { success = false, code = "INSUFFICIENT_STOCK", message = "Requested quantity is unavailable." });

        if (item is null)
        {
            cart.Items.Add(new MediLink.Core.Entities.CartItem
            {
                MedicineId = medicine.Id,
                StoreId = request.StoreId,
                MedicineName = medicine.Name,
                UnitPrice = medicine.Price,
                ImageUrl = medicine.ImageUrl,
                Quantity = request.Quantity
            });
        }
        else
        {
            item.Quantity = newQuantity;
            item.MedicineName = medicine.Name;
            item.UnitPrice = medicine.Price;
            item.ImageUrl = medicine.ImageUrl;
        }

        await _db.SaveChangesAsync();
        cart = await CartQuery() ?? cart;
        return Ok(new { success = true, message = $"{medicine.Name} added to cart.", cart = View(cart) });
    }

    [HttpPost("reconcile")]
    public async Task<IActionResult> Reconcile(MediLink.Core.DTOs.CartReconcileRequest request)
    {
        if (!IsCustomerUser() || UserId == Guid.Empty) return CustomerOnly();

        var incoming = request.Items ?? new List<MediLink.Core.DTOs.CartItemRequest>();
        if (incoming.Count > 50)
            return BadRequest(new { success = false, code = "CART_TOO_LARGE", message = "Cart contains too many different medicines." });

        // Reconcile is an explicit compatibility/recovery operation. It replaces
        // the current server cart with a validated representation supplied by the
        // current browser session. This repairs carts created by older MediLink
        // versions without touching the customer's account or order history.
        var normalized = new List<(Guid MedicineId, Guid StoreId, string ExternalMedicineId, int Quantity, InventoryMedicineDto Medicine)>();
        foreach (var item in incoming)
        {
            if (item.MedicineId == Guid.Empty || item.StoreId == Guid.Empty || item.Quantity <= 0)
                return BadRequest(new { success = false, code = "INVALID_CART_REQUEST", message = "Every cart item must have a medicine, store and positive quantity." });

            var medicine = await _inventory.GetMedicineAsync(item.MedicineId, item.StoreId);
            if (medicine is null && !string.IsNullOrWhiteSpace(item.ExternalMedicineId))
                medicine = await _inventory.GetMedicineByExternalIdAsync(item.ExternalMedicineId, item.StoreId);
            if (medicine is null || !medicine.IsActive)
                return NotFound(new { success = false, code = "STORE_OFFER_NOT_FOUND", message = "One of the medicines is no longer offered by the selected medical store." });
            if (medicine.StockQuantity < item.Quantity)
                return BadRequest(new { success = false, code = "INSUFFICIENT_STOCK", message = $"Not enough stock for {medicine.Name}." });

            normalized.Add((medicine.Id, item.StoreId, medicine.ExternalMedicineId, item.Quantity, medicine));
        }

        var storeIds = normalized.Select(x => x.StoreId).Distinct().ToList();
        if (storeIds.Count > 1)
            return Conflict(new { success = false, code = "DIFFERENT_STORE_CART", message = "One checkout can contain medicines from one medical store only." });

        var cart = await CartQuery();
        if (cart is null)
        {
            cart = new MediLink.Core.Entities.Cart { UserId = UserId };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // Clear all legacy/stale rows first. The cart itself remains owned by
            // the same customer, so no customer/order identity is lost.
            await _db.CartItems.Where(x => x.CartId == cart.Id).ExecuteDeleteAsync();
            foreach (var item in normalized)
            {
                _db.CartItems.Add(new MediLink.Core.Entities.CartItem
                {
                    CartId = cart.Id,
                    MedicineId = item.MedicineId,
                    StoreId = item.StoreId,
                    MedicineName = item.Medicine.Name,
                    UnitPrice = item.Medicine.Price,
                    ImageUrl = item.Medicine.ImageUrl,
                    Quantity = item.Quantity
                });
            }
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { success = false, code = "CART_RECONCILE_FAILED", message = "The existing customer cart could not be repaired. No order was created." });
        }

        cart = await CartQuery() ?? cart;
        return Ok(new { success = true, repaired = true, cart = View(cart) });
    }

    [HttpPut("items/{id:guid}")]
    public async Task<IActionResult> ChangeQuantity(Guid id, MediLink.Core.DTOs.CartItemRequest request)
    {
        if (!IsCustomerUser() || UserId == Guid.Empty) return CustomerOnly();
        var cart = await CartQuery();
        var item = cart?.Items.FirstOrDefault(i => i.Id == id);
        if (item is null)
            return NotFound(new { success = false, message = "Cart item was not found." });

        var medicine = await _inventory.GetMedicineAsync(item.MedicineId, item.StoreId);
        if (medicine is null || !medicine.IsActive)
            return BadRequest(new { success = false, message = "Requested medicine is unavailable." });
        if (medicine.StockQuantity < request.Quantity)
            return BadRequest(new { success = false, message = "Requested quantity is unavailable." });

        item.Quantity = request.Quantity;
        item.MedicineName = medicine.Name;
        item.UnitPrice = medicine.Price;
        item.ImageUrl = medicine.ImageUrl;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, cart = View(cart!) });
    }

    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> Remove(Guid id)
    {
        if (!IsCustomerUser() || UserId == Guid.Empty) return CustomerOnly();
        var cart = await CartQuery();
        var item = cart?.Items.FirstOrDefault(i => i.Id == id);
        if (item is null)
            return NotFound(new { success = false, message = "Cart item was not found." });

        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();
        cart = await CartQuery();
        return Ok(new { success = true, message = "Item removed.", cart = cart is null ? null : View(cart) });
    }
}
