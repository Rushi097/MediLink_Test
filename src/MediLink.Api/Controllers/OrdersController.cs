using System.Security.Claims;
using MediLink.Core.DTOs;
using MediLink.Core.Entities;
using MediLink.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Api.Controllers;

[ApiController, Route("api/orders"), Authorize(Roles = "Customer")]
public class OrdersController(MediLinkDbContext db) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(new { success = true, items = await db.Orders.Where(o => o.UserId == UserId).Include(o => o.Items).OrderByDescending(o => o.CreatedAt).ToListAsync() });

    [HttpPost]
    public async Task<IActionResult> Checkout(CheckoutRequest request)
    {
        var cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Medicine).FirstOrDefaultAsync(c => c.UserId == UserId);
        if (cart is null || cart.Items.Count == 0) return BadRequest(new { success = false, message = "Your cart is empty." });
        if (cart.Items.Any(i => !i.Medicine.IsActive || i.Medicine.StockQuantity < i.Quantity)) return BadRequest(new { success = false, message = "One or more items are unavailable." });
        var order = new Order { UserId = UserId, DeliveryAddress = request.DeliveryAddress.Trim(), TotalAmount = cart.Items.Sum(i => i.Quantity * i.Medicine.Price), Items = cart.Items.Select(i => new OrderItem { MedicineId = i.MedicineId, MedicineName = i.Medicine.Name, UnitPrice = i.Medicine.Price, Quantity = i.Quantity }).ToList() };
        foreach (var item in cart.Items) item.Medicine.StockQuantity -= item.Quantity;
        db.Orders.Add(order); db.CartItems.RemoveRange(cart.Items); await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { success = true, item = order });
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    { var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == UserId); if (order is null) return NotFound(new { success = false, message = "Order was not found." }); if (order.Status != OrderStatus.Placed) return BadRequest(new { success = false, message = "This order can no longer be cancelled." }); order.Status = OrderStatus.Cancelled; await db.SaveChangesAsync(); return Ok(new { success = true, item = order }); }
}
