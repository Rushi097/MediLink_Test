using MediLink.Core.DTOs;
using MediLink.Core.Entities;
using MediLink.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Api.Controllers;

[ApiController, Route("api/[controller]")]
public class MedicinesController(MediLinkDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? category, [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 50);
        var query = db.Medicines.AsNoTracking().Where(m => m.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(m => m.Name.Contains(search) || m.Category.Contains(search));
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(m => m.Category == category);
        var total = await query.CountAsync();
        var items = await query.OrderBy(m => m.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { success = true, items, total, page, pageSize });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id) => await db.Medicines.FindAsync(id) is { IsActive: true } medicine
        ? Ok(new { success = true, item = medicine }) : NotFound(new { success = false, message = "Medicine was not found." });

    [HttpGet("categories")]
    public async Task<IActionResult> Categories() => Ok(new { success = true, items = await db.Medicines.Where(m => m.IsActive).Select(m => m.Category).Distinct().OrderBy(c => c).ToListAsync() });

    [HttpPost, Authorize(Roles = "Admin,StoreOwner")]
    public async Task<IActionResult> Create(MedicineCreateRequest request)
    {
        var medicine = new Medicine { Name = request.Name.Trim(), Category = request.Category.Trim(), Description = request.Description.Trim(), Price = request.Price, StockQuantity = request.StockQuantity, ImageUrl = request.ImageUrl };
        db.Medicines.Add(medicine); await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = medicine.Id }, new { success = true, item = medicine });
    }

    [HttpPut("{id:guid}"), Authorize(Roles = "Admin,StoreOwner")]
    public async Task<IActionResult> Update(Guid id, MedicineCreateRequest request)
    {
        var medicine = await db.Medicines.FindAsync(id); if (medicine is null) return NotFound(new { success = false, message = "Medicine was not found." });
        medicine.Name = request.Name.Trim(); medicine.Category = request.Category.Trim(); medicine.Description = request.Description.Trim(); medicine.Price = request.Price; medicine.StockQuantity = request.StockQuantity; medicine.ImageUrl = request.ImageUrl;
        await db.SaveChangesAsync(); return Ok(new { success = true, item = medicine });
    }

    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin,StoreOwner")]
    public async Task<IActionResult> Archive(Guid id)
    { var medicine = await db.Medicines.FindAsync(id); if (medicine is null) return NotFound(new { success = false, message = "Medicine was not found." }); medicine.IsActive = false; await db.SaveChangesAsync(); return Ok(new { success = true, message = "Medicine archived." }); }
}
