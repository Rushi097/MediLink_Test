using Microsoft.EntityFrameworkCore;

namespace MediLink.Infrastructure.Data;

public static class InventorySchemaInitializer
{
    public static async Task EnsureCatalogSchemaAsync(InventoryDbContext db)
    {
        await EnsureColumnAsync(db, "Medicines", "ExternalMedicineId", "varchar(100) NULL");
        await EnsureColumnAsync(db, "Medicines", "ExternalSource", "varchar(40) NULL");
        await EnsureColumnAsync(db, "StoreInventories", "Price", "decimal(10,2) NOT NULL DEFAULT 0");
        await EnsureColumnAsync(db, "StoreInventories", "StockQuantity", "int NOT NULL DEFAULT 0");

        // Existing installations may have a legacy StoreInventories row. Keep it intact
        // rather than deleting user data; new inventory entries use the new columns.
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE StoreInventories si JOIN Medicines m ON si.MedicineId = m.Id " +
            "SET si.Price = COALESCE(NULLIF(si.Price, 0), m.Price), " +
            "si.StockQuantity = COALESCE(NULLIF(si.StockQuantity, 0), m.StockQuantity) " +
            "WHERE si.Price = 0 OR si.StockQuantity = 0;");
    }

    private static async Task EnsureColumnAsync(
        InventoryDbContext db,
        string table,
        string column,
        string definition)
    {
        var exists = await db.Database.SqlQueryRaw<int>(
            $@"SELECT COUNT(*) AS `Value`
               FROM information_schema.columns
               WHERE table_schema = DATABASE()
                 AND table_name = '{table}'
                 AND column_name = '{column}'").SingleAsync();

        if (exists == 0)
        {
            await db.Database.ExecuteSqlRawAsync(
                $"ALTER TABLE `{table}` ADD COLUMN `{column}` {definition};");
        }
    }
}
