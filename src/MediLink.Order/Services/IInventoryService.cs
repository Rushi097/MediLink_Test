namespace MediLink.Order.Services;

public interface IInventoryService
{
    Task<InventoryMedicineDto?> GetMedicineAsync(Guid medicineId, Guid storeId);
    Task<InventoryMedicineDto?> GetMedicineByExternalIdAsync(string externalMedicineId, Guid storeId);
    Task<IReadOnlyList<InventoryStoreDto>> GetStoresAsync();
    Task<IReadOnlyList<InventoryStoreItemDto>> GetStoreInventoryAsync(Guid storeId);
    Task<InventorySummaryDto> GetInventorySummaryAsync();
    Task<bool> ReserveStockAsync(Guid medicineId, Guid storeId, int quantity, Guid reservationId);
    Task<bool> RestockAsync(Guid medicineId, Guid storeId, int quantity, Guid reservationId);
}
