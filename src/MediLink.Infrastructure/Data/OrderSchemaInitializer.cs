using Microsoft.EntityFrameworkCore;

namespace MediLink.Infrastructure.Data;

public static class OrderSchemaInitializer
{
    public static async Task EnsureStoreColumnsAsync(OrderDbContext db)
    {
        await EnsureColumnAsync(db, "CartItems", "StoreId", "char(36) COLLATE ascii_general_ci NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'");
        await EnsureColumnAsync(db, "OrderItems", "StoreId", "char(36) COLLATE ascii_general_ci NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'");
        await EnsureColumnAsync(db, "Orders", "PaymentMethod", "varchar(30) NOT NULL DEFAULT 'CashOnDelivery'");
        await EnsureColumnAsync(db, "Orders", "ReservationId", "char(36) COLLATE ascii_general_ci NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'");
        await EnsureColumnAsync(db, "StoreOrderAssignments", "StoreName", "varchar(200) NOT NULL DEFAULT ''");
        await EnsureColumnAsync(db, "StoreOrderAssignments", "StoreAddress", "varchar(500) NOT NULL DEFAULT ''");

        // One-time compatibility cleanup for customers who used the cart
        // before store selection became mandatory. Such rows have no pharmacy
        // owner and must not block the customer's next store-specific cart.
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM `CartItems` WHERE `StoreId` = '00000000-0000-0000-0000-000000000000'");
    }

    private static async Task EnsureColumnAsync(
        OrderDbContext db,
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
            await db.Database.ExecuteSqlRawAsync($"ALTER TABLE `{table}` ADD COLUMN `{column}` {definition};");
    }
}
