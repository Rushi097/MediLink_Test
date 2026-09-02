using MediLink.Core.DTOs;
using MediLink.Inventory.DTOs;
using MediLink.Inventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediLink.Inventory.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoresController : ControllerBase
{
    private readonly FlatFileInventoryStore _files;
    private readonly IMedicineCatalogService _catalog;
    public StoresController(FlatFileInventoryStore files, IMedicineCatalogService catalog) => (_files, _catalog) = (files, catalog);

    private bool InternalRequest() => Request.Headers.TryGetValue("X-MediLink-Internal-Key", out var key) && key == (Environment.GetEnvironmentVariable("MEDILINK_INTERNAL_KEY") ?? "medilink-internal-development-key");

    [HttpPost("internal")]
    public async Task<IActionResult> CreateInternal([FromBody] CreateStoreRequest request, CancellationToken ct)
    {
        if (!InternalRequest()) return Unauthorized(new { success = false, message = "Internal service authentication required." });
        if (request.StoreOwnerProfileId == Guid.Empty) return BadRequest(new { success = false, message = "Store owner profile is required." });
        try
        {
            var store = await _files.CreateStoreAsync(request.Name, request.Address, request.StoreOwnerProfileId, ct);
            return Created($"api/stores/{store.Id}", new { success = true, item = new { store.Id, store.Name, store.Address, StoreOwnerProfileId = store.StoreOwnerProfileId } });
        }
        catch (InvalidOperationException ex) { return Conflict(new { success = false, message = ex.Message }); }
    }

    [HttpGet("owner/me"), Authorize(Roles = "StoreOwner")]
    public async Task<IActionResult> GetMyStore(CancellationToken ct)
    {
        var owner = User.FindFirst("StoreOwnerProfileId")?.Value;
        if (!Guid.TryParse(owner, out var ownerId)) return Forbid();
        var store = await _files.GetStoreByOwnerAsync(ownerId, ct);
        return store is null ? NotFound(new { success = false, message = "No store is registered for this account." }) : Ok(new { success = true, item = store });
    }

    [HttpGet("internal/owner/{ownerProfileId:guid}")]
    public async Task<IActionResult> GetByOwnerInternal(Guid ownerProfileId, CancellationToken ct)
    {
        if (!InternalRequest()) return Unauthorized(new { success = false, message = "Internal service authentication required." });
        var store = await _files.GetStoreByOwnerAsync(ownerProfileId, ct);
        return store is null ? NotFound(new { success = false, message = "Store not found." }) : Ok(new { success = true, item = store });
    }

    [HttpPost("{storeId:guid}/inventory"), Authorize(Roles = "StoreOwner,Admin")]
    public async Task<IActionResult> AddMedicine(Guid storeId, [FromBody] StoreInventoryCreateRequest request, CancellationToken ct)
    {
        var store = await _files.GetStoreAsync(storeId, ct);
        if (store is null) return NotFound(new { success = false, message = "Store not found." });
        if (!CanManage(store)) return Forbid();
        var metadata = await _catalog.GetByExternalIdAsync(request.ExternalMedicineId.Trim(), ct);
        if (metadata is null) return BadRequest(new { success = false, message = "The selected medicine could not be verified by the catalogue." });
        var medicine = (await _files.GetCatalogAsync(null, ct)).FirstOrDefault(x => string.Equals(x.Id, metadata.ExternalId, StringComparison.OrdinalIgnoreCase));
        if (medicine is null) medicine = await CreateReferenceFromMetadata(metadata, ct);
        try
        {
            var row = await _files.AddInventoryAsync(storeId, medicine, request.Price, request.StockQuantity, ct);
            return Ok(new { success = true, item = ToStoreItem(store, medicine, metadata, row) });
        }
        catch (InvalidOperationException ex) { return Conflict(new { success = false, message = ex.Message }); }
    }

    [HttpPut("{storeId:guid}/inventory/{medicineId:guid}"), Authorize(Roles = "StoreOwner,Admin")]
    public async Task<IActionResult> UpdateMedicine(Guid storeId, Guid medicineId, [FromBody] StoreInventoryUpdateRequest request, CancellationToken ct)
    {
        var store = await _files.GetStoreAsync(storeId, ct);
        if (store is null) return NotFound(new { success = false, message = "Store not found." });
        if (!CanManage(store)) return Forbid();
        var row = await _files.UpdateInventoryAsync(storeId, medicineId, request.Price, request.StockQuantity, ct);
        if (row is null) return NotFound(new { success = false, message = "Medicine is not listed in this store." });
        var medicine = (await _files.GetCatalogAsync(null, ct)).FirstOrDefault(x => x.InternalId == medicineId);
        if (medicine is null) return NotFound(new { success = false, message = "Medicine reference not found." });
        var metadata = await _catalog.GetByExternalIdAsync(medicine.Id, ct);
        if (metadata is null)
        {
            return NotFound(new
            {
                success = false,
                message = "Medicine catalogue information not found."
            });
        }

        var item = ToStoreItem(store, medicine, metadata, row);

        return Ok(new
        {
            success = true,
            item
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetStores(CancellationToken ct) => Ok((await _files.GetStoresAsync(ct)).Select(s => new InventoryStoreDto { Id=s.Id, Name=s.Name, Address=s.Address, OwnerId=s.StoreOwnerProfileId }).OrderBy(s => s.Name));

    [HttpGet("{storeId:guid}/inventory")]
    public async Task<IActionResult> GetStoreInventory(Guid storeId, CancellationToken ct)
    {
        var store = await _files.GetStoreAsync(storeId, ct);
        if (store is null) return NotFound(new { success = false, message = "Store not found." });
        var catalog = await _files.GetCatalogAsync(null, ct);
        var rows = await _files.GetStoreInventoryAsync(storeId, ct);
        var items = new List<InventoryStoreItemDto>();
        foreach (var row in rows.OrderBy(x => catalog.FirstOrDefault(m => m.InternalId == x.MedicineId)?.Name))
        {
            var medicine = catalog.FirstOrDefault(m => m.InternalId == row.MedicineId); if (medicine is null) continue;
            var metadata = await _catalog.GetByExternalIdAsync(medicine.Id, ct); if (metadata is null) continue;
            items.Add(ToStoreItem(store, medicine, metadata, row));
        }
        return Ok(new { success = true, items });
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var stores = await _files.GetStoresAsync(ct); var catalog = await _files.GetCatalogAsync(null, ct);
        var all = new List<(StoreRecord Store, StoreInventoryRecord Row, MedicineRecord Medicine)>();
        foreach (var store in stores)
        foreach (var row in await _files.GetStoreInventoryAsync(store.Id, ct))
        {
            var med = catalog.FirstOrDefault(x => x.InternalId == row.MedicineId); if (med is not null) all.Add((store,row,med));
        }
        var low = all.Where(x => x.Row.StockQuantity <= 10).OrderBy(x => x.Row.StockQuantity).Take(8).Select(x => new InventoryStoreItemDto { Id=x.Row.Id, StoreId=x.Store.Id, MedicineId=x.Medicine.InternalId, ExternalMedicineId=x.Medicine.Id, Name=x.Medicine.Name, Category=x.Medicine.Category, Description=x.Medicine.Description, Price=x.Row.Price, StockQuantity=x.Row.StockQuantity, ImageUrl=x.Medicine.ImageUrl }).ToList();
        return Ok(new InventorySummaryDto { ActiveProducts=all.Count, LowStock=all.Count(x=>x.Row.StockQuantity<=10), InventoryValue=all.Sum(x=>x.Row.Price*x.Row.StockQuantity), LowStockItems=low });
    }

    private bool CanManage(StoreRecord store)
    {
        if (User.IsInRole("Admin")) return true;
        return Guid.TryParse(User.FindFirst("StoreOwnerProfileId")?.Value, out var owner) && owner == store.StoreOwnerProfileId;
    }

    private async Task<MedicineRecord> CreateReferenceFromMetadata(MedicineCatalogItemDto metadata, CancellationToken ct)
    {
        var row = new MedicineRecord { Id=metadata.ExternalId, ExternalSource=metadata.ExternalSource, Name=metadata.DisplayName, Category=metadata.Category, Description=metadata.Description, ImageUrl=metadata.ImageUrl, IsActive=true };
        await _files.UpsertMedicineAsync(row, ct); return row;
    }

    private static InventoryStoreItemDto ToStoreItem(StoreRecord store, MedicineRecord medicine, MedicineCatalogItemDto metadata, StoreInventoryRecord row) => new()
    {
        Id=row.Id, StoreId=store.Id, MedicineId=medicine.InternalId, ExternalMedicineId=medicine.Id, Name=metadata.DisplayName,
        Category=metadata.Category, Description=metadata.Description, Price=row.Price, StockQuantity=row.StockQuantity, ImageUrl=metadata.ImageUrl
    };

    public sealed record CreateStoreRequest(string Name, string Address, Guid StoreOwnerProfileId);
}
