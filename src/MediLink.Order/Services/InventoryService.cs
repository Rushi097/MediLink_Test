using System.Net.Http.Json;

namespace MediLink.Order.Services;

public class InventoryService : IInventoryService
{
    private const string InternalKeyHeader = "X-MediLink-Internal-Key";
    private readonly string _internalKey;
    private readonly HttpClient _client;

    public InventoryService(HttpClient client)
    {
        _client = client;
        _internalKey = Environment.GetEnvironmentVariable("MEDILINK_INTERNAL_KEY") ?? "medilink-internal-development-key";
    }

    public async Task<InventoryMedicineDto?> GetMedicineAsync(Guid medicineId, Guid storeId)
    {
        var response = await _client.GetAsync($"api/medicines/offer/{storeId}/{medicineId}");
        if (!response.IsSuccessStatusCode)
            return null;

        var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<InventoryMedicineDto>>();
        return wrapper?.Item;
    }

    public async Task<InventoryMedicineDto?> GetMedicineByExternalIdAsync(string externalMedicineId, Guid storeId)
    {
        if (string.IsNullOrWhiteSpace(externalMedicineId)) return null;
        var url = $"api/medicines/offer-by-external/{storeId}?externalMedicineId={Uri.EscapeDataString(externalMedicineId)}";
        var response = await _client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<InventoryMedicineDto>>();
        return wrapper?.Item;
    }

    public async Task<IReadOnlyList<InventoryStoreDto>> GetStoresAsync()
    {
        var stores = await _client.GetFromJsonAsync<IReadOnlyList<InventoryStoreDto>>("api/stores");
        return stores ?? Array.Empty<InventoryStoreDto>();
    }

    public async Task<IReadOnlyList<InventoryStoreItemDto>> GetStoreInventoryAsync(Guid storeId)
    {
        var response = await _client.GetFromJsonAsync<ApiListResponse<InventoryStoreItemDto>>($"api/stores/{storeId}/inventory");
        return response?.Items ?? new List<InventoryStoreItemDto>();
    }

    public async Task<InventorySummaryDto> GetInventorySummaryAsync()
    {
        var summary = await _client.GetFromJsonAsync<InventorySummaryDto>("api/stores/summary");
        return summary ?? new InventorySummaryDto();
    }

    public async Task<bool> ReserveStockAsync(Guid medicineId, Guid storeId, int quantity, Guid reservationId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/medicines/{medicineId}/reserve")
        {
            Content = JsonContent.Create(new { StoreId = storeId, Quantity = quantity, ReservationId = reservationId })
        };
        request.Headers.TryAddWithoutValidation(InternalKeyHeader, _internalKey);
        var response = await _client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RestockAsync(Guid medicineId, Guid storeId, int quantity, Guid reservationId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/medicines/{medicineId}/restock")
        {
            Content = JsonContent.Create(new { StoreId = storeId, Quantity = quantity, ReservationId = reservationId })
        };
        request.Headers.TryAddWithoutValidation(InternalKeyHeader, _internalKey);
        var response = await _client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    private sealed class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Item { get; set; }
    }

    private sealed class ApiListResponse<T>
    {
        public bool Success { get; set; }
        public List<T> Items { get; set; } = new();
    }
}
