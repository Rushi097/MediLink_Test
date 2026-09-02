namespace MediLink.Inventory.DTOs;

public class InventoryMedicineDto
{
    public Guid Id { get; set; }
    public string ExternalMedicineId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Medicine";
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public Guid StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}

public class InventoryStoreDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
}

public class InventoryStoreItemDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Guid MedicineId { get; set; }
    public string ExternalMedicineId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Medicine";
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
}

public class InventorySummaryDto
{
    public int ActiveProducts { get; set; }
    public int LowStock { get; set; }
    public decimal InventoryValue { get; set; }
    public IReadOnlyList<InventoryStoreItemDto> LowStockItems { get; set; } = Array.Empty<InventoryStoreItemDto>();
}
