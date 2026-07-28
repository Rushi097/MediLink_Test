using System.Security.Claims;
using MediLink.Core.DTOs;
using MediLink.Core.Entities;
using MediLink.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Api.Controllers;

[ApiController, Route("api/cart"), Authorize(Roles = "Customer")]
public class CartController(MediLinkDbContext db) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Task<Cart?> CartQuery() => db.Carts.Include(c => c.Items).ThenInclude(i => i.Medicine).FirstOrDefaultAsync(c => c.UserId == UserId);
    private static object View(Cart cart) => new { cart.Id, items = cart.Items.Select(i => new { i.Id, i.MedicineId, name = i.Medicine.Name, price = i.Medicine.Price, i.Quantity, subtotal = i.Quantity * i.Medicine.Price }), total = cart.Items.Sum(i => i.Quantity * i.Medicine.Price) };

    [HttpGet]
    public async Task<IActionResult> Get() { var cart = await CartQuery(); return Ok(new { success = true, cart = cart is null ? null : View(cart) }); }

    [HttpPost("items")]
    public async Task<IActionResult> Add(CartItemRequest request)
    {
        var medicine = await db.Medicines.FindAsync(request.MedicineId);
        if (medicine is null || !medicine.IsActive) return NotFound(new { success = false, message = "Medicine was not found." });
        if (medicine.StockQuantity < request.Quantity) return BadRequest(new { success = false, message = "Requested quantity is unavailable." });
        var cart = await CartQuery();
        if (cart is null) { cart = new Cart { UserId = UserId }; db.Carts.Add(cart); }
        var item = cart.Items.FirstOrDefault(i => i.MedicineId == request.MedicineId);
        if (item is null) cart.Items.Add(new CartItem { MedicineId = request.MedicineId, Quantity = request.Quantity }); else item.Quantity += request.Quantity;
        await db.SaveChangesAsync(); cart = await CartQuery(); return Ok(new { success = true, cart = View(cart!) });
    }

    [HttpPut("items/{id:guid}")]
    public async Task<IActionResult> ChangeQuantity(Guid id, CartItemRequest request)
    { var cart = await CartQuery(); var item = cart?.Items.FirstOrDefault(i => i.Id == id); if (item is null) return NotFound(new { success = false, message = "Cart item was not found." }); if (item.Medicine.StockQuantity < request.Quantity) return BadRequest(new { success = false, message = "Requested quantity is unavailable." }); item.Quantity = request.Quantity; await db.SaveChangesAsync(); return Ok(new { success = true, cart = View(cart!) }); }

    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> Remove(Guid id)
    { var cart = await CartQuery(); var item = cart?.Items.FirstOrDefault(i => i.Id == id); if (item is null) return NotFound(new { success = false, message = "Cart item was not found." }); db.CartItems.Remove(item); await db.SaveChangesAsync(); return Ok(new { success = true, message = "Item removed." }); }
}
