namespace MediLink.Core.Entities;

public class StoreInventory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public Guid MedicineId { get; set; }
    public Medicine Medicine { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
