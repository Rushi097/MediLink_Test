namespace MediLink.Core.Entities;

public class Medicine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Reference data only. The authoritative medicine metadata comes from the external
    // medicine catalogue (RxNorm/DailyMed); this table keeps the stable reference used
    // by local store inventory and orders.
    public string ExternalMedicineId { get; set; } = string.Empty;
    public string ExternalSource { get; set; } = "RxNorm";
    public string Name { get; set; } = string.Empty;

    // Legacy fields retained for backward-compatible database upgrades. New APIs do not
    // accept or update these values; store-specific price/stock live in StoreInventory.
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
