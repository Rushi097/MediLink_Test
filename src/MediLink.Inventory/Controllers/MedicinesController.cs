using MediLink.Core.DTOs;
using MediLink.Inventory.DTOs;
using MediLink.Inventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediLink.Inventory.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicinesController : ControllerBase
{
    private readonly FlatFileInventoryStore _files;
    private readonly IMedicineCatalogService _catalog;
    public MedicinesController(FlatFileInventoryStore files, IMedicineCatalogService catalog) => (_files, _catalog) = (files, catalog);

    private bool InternalRequest() => Request.Headers.TryGetValue("X-MediLink-Internal-Key", out var key) &&
        key == (Environment.GetEnvironmentVariable("MEDILINK_INTERNAL_KEY") ?? "medilink-internal-development-key");
    private IActionResult InternalDenied() => Unauthorized(new { success = false, message = "Internal service authentication required." });

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] Guid? storeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 24, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 50);
        var catalog = await _files.GetCatalogAsync(search, ct);
        var total = catalog.Count;
        var pageItems = catalog.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var items = new List<InventoryMedicineDto>();

        foreach (var medicine in pageItems)
        {
            var metadata = await _catalog.GetByExternalIdAsync(medicine.Id, ct);
            if (metadata is null) continue;
            var stores = await _files.GetStoresAsync(ct);
            var candidateStores = storeId.HasValue ? stores.Where(s => s.Id == storeId.Value) : stores;
            var foundOffer = false;
            foreach (var store in candidateStores)
            {
                var offer = await _files.GetOfferAsync(store.Id, medicine.InternalId, ct);
                if (offer is null) continue;
                foundOffer = true;
                items.Add(ToDto(medicine, metadata, store, offer));
            }
            if (!foundOffer)
            {
                items.Add(new InventoryMedicineDto
                {
                    Id = medicine.InternalId, ExternalMedicineId = medicine.Id, Name = metadata.DisplayName,
                    Category = metadata.Category, Description = metadata.Description, ImageUrl = metadata.ImageUrl,
                    IsActive = true, StoreId = Guid.Empty, StoreName = "", Price = 0, StockQuantity = 0
                });
            }
        }

        if (items.Count == 0 && !string.IsNullOrWhiteSpace(search) && !storeId.HasValue)
        {
            var external = await _catalog.SearchAsync(search, ct);
            items = external.Select(x => new InventoryMedicineDto
            {
                Id = Guid.Empty, ExternalMedicineId = x.ExternalId, Name = x.DisplayName, Category = x.Category,
                Description = x.Description, ImageUrl = x.ImageUrl, IsActive = true, StoreId = Guid.Empty, StoreName = "", Price = 0, StockQuantity = 0
            }).ToList();
            total = items.Count;
        }
        return Ok(new { success = true, items, total, page, pageSize });
    }

    [HttpGet("offer/{storeId:guid}/{medicineId:guid}")]
    public async Task<IActionResult> GetOffer(Guid storeId, Guid medicineId, CancellationToken ct)
    {
        var medicine = (await _files.GetCatalogAsync(null, ct)).FirstOrDefault(x => x.InternalId == medicineId);
        var store = await _files.GetStoreAsync(storeId, ct);
        var offer = await _files.GetOfferAsync(storeId, medicineId, ct);
        if (medicine is null || store is null || offer is null) return NotFound(new { success = false, message = "Medicine is not stocked by this store." });
        var metadata = await _catalog.GetByExternalIdAsync(medicine.Id, ct);
        return metadata is null ? NotFound(new { success = false, message = "Medicine metadata is unavailable." }) : Ok(new { success = true, item = ToDto(medicine, metadata, store, offer) });
    }

    [HttpGet("offer-by-external/{storeId:guid}")]
    public async Task<IActionResult> GetOfferByExternal(Guid storeId, [FromQuery] string externalMedicineId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(externalMedicineId))
            return BadRequest(new { success = false, message = "External medicine id is required." });
        var medicine = await _files.GetMedicineAsync(externalMedicineId, ct);
        var store = await _files.GetStoreAsync(storeId, ct);
        if (medicine is null || store is null) return NotFound(new { success = false, message = "Medicine is not stocked by this store." });
        var offer = await _files.GetOfferAsync(storeId, medicine.InternalId, ct);
        if (offer is null) return NotFound(new { success = false, message = "Medicine is not stocked by this store." });
        var metadata = await _catalog.GetByExternalIdAsync(medicine.Id, ct);
        return metadata is null ? NotFound(new { success = false, message = "Medicine metadata is unavailable." }) : Ok(new { success = true, item = ToDto(medicine, metadata, store, offer) });
    }

    [HttpPost("{medicineId:guid}/reserve")]
    public async Task<IActionResult> Reserve(Guid medicineId, [FromBody] QuantityAdjustmentRequest request, CancellationToken ct)
    {
        if (!InternalRequest()) return InternalDenied();
        if (request.Quantity <= 0 || request.StoreId == Guid.Empty) return BadRequest(new { success = false, message = "Store and quantity are required." });
        var ok = await _files.ReserveStockAsync(request.StoreId, medicineId, request.Quantity, request.ReservationId, ct);
        return ok ? Ok(new { success = true }) : BadRequest(new { success = false, message = "Not enough stock available or medicine is not stocked by this store." });
    }

    [HttpPost("{medicineId:guid}/restock")]
    public async Task<IActionResult> Restock(Guid medicineId, [FromBody] QuantityAdjustmentRequest request, CancellationToken ct)
    {
        if (!InternalRequest()) return InternalDenied();
        if (request.Quantity <= 0 || request.StoreId == Guid.Empty) return BadRequest(new { success = false, message = "Store and quantity are required." });
        var ok = await _files.ReleaseReservationAsync(request.StoreId, medicineId, request.Quantity, request.ReservationId, ct);
        return ok ? Ok(new { success = true }) : NotFound(new { success = false, message = "Medicine is not stocked by this store." });
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories(CancellationToken ct)
    {
        var items = (await _files.GetCatalogAsync(null, ct)).Select(x => x.Category).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        return Ok(new { success = true, items });
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public IActionResult Create(MedicineCreateRequest request) => BadRequest(new { success = false, message = "Medicine master records are managed by the MediLink catalogue. Store owners select a medicine and enter only price and stock." });

    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        var ok = await _files.SetMedicineActiveAsync(id, false, ct);
        return ok ? Ok(new { success = true, message = "Medicine reference archived." }) : NotFound(new { success = false, message = "Medicine was not found." });
    }

    private static InventoryMedicineDto ToDto(MedicineRecord medicine, MedicineCatalogItemDto metadata, StoreRecord store, StoreInventoryRecord offer) => new()
    {
        Id = medicine.InternalId, ExternalMedicineId = medicine.Id, Name = metadata.DisplayName, Category = metadata.Category,
        Description = metadata.Description, ImageUrl = metadata.ImageUrl, IsActive = medicine.IsActive,
        StoreId = store.Id, StoreName = store.Name, Price = offer.Price, StockQuantity = offer.StockQuantity
    };

    public sealed class QuantityAdjustmentRequest { public Guid StoreId { get; set; } public int Quantity { get; set; } public Guid ReservationId { get; set; } }
}
