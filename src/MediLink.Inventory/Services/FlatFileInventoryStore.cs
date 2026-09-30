using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediLink.Inventory.Services;

/// <summary>
/// File-backed inventory store. The inventory service owns these files and no other
/// service reads them directly. Each registered store gets its own JSON inventory file.
/// </summary>
public sealed class FlatFileInventoryStore
{
    private readonly string _root;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FlatFileInventoryStore(IWebHostEnvironment env, IConfiguration config)
    {
        _root = Path.GetFullPath(config["InventoryFiles:Root"] ?? Path.Combine(env.ContentRootPath, "data"));
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(Path.Combine(_root, "inventory"));
    }

    public string Root => _root;

    private string CatalogPath => Path.Combine(_root, "catalog.json");
    private string StoresPath => Path.Combine(_root, "stores.json");
    private string ReservationsPath => Path.Combine(_root, "reservations.json");
    private string StoreInventoryPath(Guid storeId) => Path.Combine(_root, "inventory", $"{storeId:N}.json");

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (!File.Exists(CatalogPath))
            {
                var source = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");
                if (File.Exists(source)) File.Copy(source, CatalogPath, false);
                else await WriteAsync(CatalogPath, new List<MedicineRecord>(), ct);
            }
            if (!File.Exists(StoresPath)) await WriteAsync(StoresPath, new List<StoreRecord>(), ct);
            if (!File.Exists(ReservationsPath)) await WriteAsync(ReservationsPath, new List<StockReservationRecord>(), ct);
        }
        finally { _gate.Release(); }
    }

    public async Task<List<MedicineRecord>> GetCatalogAsync(string? search, CancellationToken ct = default)
    {
        var all = await ReadAsync<List<MedicineRecord>>(CatalogPath, ct) ?? new();
        if (string.IsNullOrWhiteSpace(search)) return all.Where(x => x.IsActive).OrderBy(x => x.Name).ToList();
        var term = search.Trim();
        return all.Where(x => x.IsActive && (x.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || x.Id.Contains(term, StringComparison.OrdinalIgnoreCase)))
                  .OrderBy(x => x.Name).ToList();
    }

    public async Task<bool> SetMedicineActiveAsync(Guid internalId, bool active, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var rows = await ReadAsync<List<MedicineRecord>>(CatalogPath, ct) ?? new();
            var row = rows.FirstOrDefault(x => x.InternalId == internalId);
            if (row is null) return false;
            row.IsActive = active;
            await WriteAsync(CatalogPath, rows, ct);
            return true;
        }
        finally { _gate.Release(); }
    }

    public async Task UpsertMedicineAsync(MedicineRecord medicine, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var rows = await ReadAsync<List<MedicineRecord>>(CatalogPath, ct) ?? new();
            var existing = rows.FindIndex(x => string.Equals(x.Id, medicine.Id, StringComparison.OrdinalIgnoreCase));
            if (existing >= 0) rows[existing] = medicine; else rows.Add(medicine);
            await WriteAsync(CatalogPath, rows, ct);
        }
        finally { _gate.Release(); }
    }

    public async Task<MedicineRecord?> GetMedicineAsync(string externalId, CancellationToken ct = default)
    {
        var all = await ReadAsync<List<MedicineRecord>>(CatalogPath, ct) ?? new();
        return all.FirstOrDefault(x => x.IsActive && string.Equals(x.Id, externalId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<StoreRecord>> GetStoresAsync(CancellationToken ct = default) =>
        await ReadAsync<List<StoreRecord>>(StoresPath, ct) ?? new();

    public async Task<StoreRecord?> GetStoreAsync(Guid storeId, CancellationToken ct = default) =>
        (await GetStoresAsync(ct)).FirstOrDefault(x => x.Id == storeId);

    public async Task<StoreRecord?> GetStoreByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        (await GetStoresAsync(ct)).FirstOrDefault(x => x.StoreOwnerProfileId == ownerId);

    public async Task<StoreRecord> CreateStoreAsync(string name, string address, Guid ownerId, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var stores = await ReadAsync<List<StoreRecord>>(StoresPath, ct) ?? new();
            if (stores.Any(x => x.StoreOwnerProfileId == ownerId)) throw new InvalidOperationException("A store already exists for this owner.");
            var store = new StoreRecord(Guid.NewGuid(), name.Trim(), address.Trim(), ownerId);
            stores.Add(store);
            await WriteAsync(StoresPath, stores, ct);
            await WriteAsync(StoreInventoryPath(store.Id), new List<StoreInventoryRecord>(), ct);
            return store;
        }
        finally { _gate.Release(); }
    }

    public async Task<List<StoreInventoryRecord>> GetStoreInventoryAsync(Guid storeId, CancellationToken ct = default) =>
        await ReadAsync<List<StoreInventoryRecord>>(StoreInventoryPath(storeId), ct) ?? new();

    public async Task<StoreInventoryRecord?> GetOfferAsync(Guid storeId, Guid medicineId, CancellationToken ct = default)
    {
        var rows = await GetStoreInventoryAsync(storeId, ct);
        return rows.FirstOrDefault(x => x.MedicineId == medicineId);
    }

    public async Task<StoreInventoryRecord?> GetOfferByExternalIdAsync(Guid storeId, string externalId, CancellationToken ct = default)
    {
        var medicine = await GetMedicineAsync(externalId, ct);
        if (medicine is null) return null;
        return await GetOfferAsync(storeId, medicine.InternalId, ct);
    }

    public async Task<StoreInventoryRecord> AddInventoryAsync(Guid storeId, MedicineRecord medicine, decimal price, int stock, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var path = StoreInventoryPath(storeId);
            var rows = await ReadAsync<List<StoreInventoryRecord>>(path, ct) ?? new();
            if (rows.Any(x => x.MedicineId == medicine.InternalId)) throw new InvalidOperationException("This medicine is already in the store inventory. Update its price or stock instead.");
            var row = new StoreInventoryRecord(Guid.NewGuid(), storeId, medicine.InternalId, medicine.Id, price, stock);
            rows.Add(row);
            await WriteAsync(path, rows, ct);
            return row;
        }
        finally { _gate.Release(); }
    }

    public async Task<StoreInventoryRecord?> UpdateInventoryAsync(Guid storeId, Guid medicineId, decimal price, int stock, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var path = StoreInventoryPath(storeId);
            var rows = await ReadAsync<List<StoreInventoryRecord>>(path, ct) ?? new();
            var row = rows.FirstOrDefault(x => x.MedicineId == medicineId);
            if (row is null) return null;
            row.Price = price; row.StockQuantity = stock;
            await WriteAsync(path, rows, ct);
            return row;
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> AdjustStockAsync(Guid storeId, Guid medicineId, int delta, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var path = StoreInventoryPath(storeId);
            var rows = await ReadAsync<List<StoreInventoryRecord>>(path, ct) ?? new();
            var row = rows.FirstOrDefault(x => x.MedicineId == medicineId);
            if (row is null || row.StockQuantity + delta < 0) return false;
            row.StockQuantity += delta;
            await WriteAsync(path, rows, ct);
            return true;
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// Atomically decreases available stock and records an idempotent reservation.
    /// Cart operations never call this; checkout is the point at which stock is reserved.
    /// </summary>
    public async Task<bool> ReserveStockAsync(Guid storeId, Guid medicineId, int quantity, Guid reservationId, CancellationToken ct = default)
    {
        if (quantity <= 0 || reservationId == Guid.Empty) return false;
        await _gate.WaitAsync(ct);
        try
        {
            var reservations = await ReadAsync<List<StockReservationRecord>>(ReservationsPath, ct) ?? new();
            if (reservations.Any(x => x.ReservationId == reservationId && x.StoreId == storeId && x.MedicineId == medicineId))
                return true;

            var path = StoreInventoryPath(storeId);
            var rows = await ReadAsync<List<StoreInventoryRecord>>(path, ct) ?? new();
            var row = rows.FirstOrDefault(x => x.MedicineId == medicineId);
            if (row is null || row.StockQuantity < quantity) return false;

            row.StockQuantity -= quantity;
            reservations.Add(new StockReservationRecord(reservationId, storeId, medicineId, quantity, DateTime.UtcNow));
            try
            {
                await WriteAsync(path, rows, ct);
                await WriteAsync(ReservationsPath, reservations, ct);
                return true;
            }
            catch
            {
                // Best-effort compensation if the second flat-file write fails.
                row.StockQuantity += quantity;
                reservations.RemoveAll(x => x.ReservationId == reservationId && x.StoreId == storeId && x.MedicineId == medicineId);
                try { await WriteAsync(path, rows, ct); } catch { }
                try { await WriteAsync(ReservationsPath, reservations, ct); } catch { }
                return false;
            }
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// Releases an existing reservation exactly once. Repeating the request is safe.
    /// </summary>
    public async Task<bool> ReleaseReservationAsync(Guid storeId, Guid medicineId, int quantity, Guid reservationId, CancellationToken ct = default)
    {
        if (reservationId == Guid.Empty) return await AdjustStockAsync(storeId, medicineId, quantity, ct);
        await _gate.WaitAsync(ct);
        try
        {
            var reservations = await ReadAsync<List<StockReservationRecord>>(ReservationsPath, ct) ?? new();
            var reservation = reservations.FirstOrDefault(x => x.ReservationId == reservationId && x.StoreId == storeId && x.MedicineId == medicineId);
            if (reservation is null) return true;

            var path = StoreInventoryPath(storeId);
            var rows = await ReadAsync<List<StoreInventoryRecord>>(path, ct) ?? new();
            var row = rows.FirstOrDefault(x => x.MedicineId == medicineId);
            if (row is null) return false;
            row.StockQuantity += reservation.Quantity;
            reservations.Remove(reservation);
            try
            {
                await WriteAsync(path, rows, ct);
                await WriteAsync(ReservationsPath, reservations, ct);
                return true;
            }
            catch
            {
                // Restore the in-memory reservation if persistence is interrupted.
                row.StockQuantity -= reservation.Quantity;
                reservations.Add(reservation);
                try { await WriteAsync(path, rows, ct); } catch { }
                try { await WriteAsync(ReservationsPath, reservations, ct); } catch { }
                return false;
            }
        }
        finally { _gate.Release(); }
    }

    private async Task<T?> ReadAsync<T>(string path, CancellationToken ct)
    {
        if (!File.Exists(path)) return default;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, _json, ct);
    }

    private async Task WriteAsync<T>(string path, T value, CancellationToken ct)
    {
        var temp = path + ".tmp";
        await using (var stream = File.Create(temp)) await JsonSerializer.SerializeAsync(stream, value, _json, ct);
        File.Move(temp, path, true);
    }
}

public sealed record StockReservationRecord(Guid ReservationId, Guid StoreId, Guid MedicineId, int Quantity, DateTime ReservedAtUtc);

public sealed class MedicineRecord
{
    public Guid InternalId { get; set; } = Guid.NewGuid();
    public string Id { get; set; } = "";
    public string ExternalSource { get; set; } = "MediLinkCatalog";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "Medicine";
    public string Description { get; set; } = "";
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record StoreRecord(Guid Id, string Name, string Address, Guid StoreOwnerProfileId);

public sealed class StoreInventoryRecord
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Guid MedicineId { get; set; }
    public string ExternalMedicineId { get; set; } = "";
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }

    public StoreInventoryRecord() { }
    public StoreInventoryRecord(Guid id, Guid storeId, Guid medicineId, string externalMedicineId, decimal price, int stockQuantity)
        => (Id, StoreId, MedicineId, ExternalMedicineId, Price, StockQuantity) = (id, storeId, medicineId, externalMedicineId, price, stockQuantity);
}
