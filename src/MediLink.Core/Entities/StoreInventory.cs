namespace MediLink.Core.Entities;

public class StoreInventory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StoreId { get; set; }
    public Store Store { get; set; } = null!;

    // Local reference to an external medicine concept. The medicine metadata itself is
    // resolved by Inventory's Medicine Catalog API.
    public Guid MedicineId { get; set; }
    public Medicine Medicine { get; set; } = null!;

    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
