package com.medilink.store.service;

import com.medilink.store.entity.Medicine;
import com.medilink.store.entity.Store;
import com.medilink.store.web.ProductForm;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.sql.Timestamp;
import java.time.LocalDateTime;
import java.util.List;
import java.util.UUID;

/** Writes inventory IDs as MySQL char(36) values, matching the EF Core schema. */
@Service
public class StoreInventoryService {
    private final JdbcTemplate jdbc;

    public StoreInventoryService(JdbcTemplate jdbc) {
        this.jdbc = jdbc;
    }

    public List<Medicine> listProducts(UUID storeId) {
        return jdbc.query("""
                SELECT m.Id, m.Name, m.Category, m.Description, m.Price, m.StockQuantity,
                       m.ImageUrl, m.IsActive, m.CreatedAt
                FROM StoreInventories inventory
                INNER JOIN Medicines m ON m.Id = inventory.MedicineId
                WHERE inventory.StoreId = ?
                ORDER BY m.CreatedAt DESC
                """, (rows, rowNumber) -> new Medicine(
                        UUID.fromString(rows.getString("Id")),
                        rows.getString("Name"),
                        rows.getString("Category"),
                        rows.getString("Description"),
                        rows.getBigDecimal("Price"),
                        rows.getInt("StockQuantity"),
                        rows.getString("ImageUrl"),
                        rows.getBoolean("IsActive"),
                        rows.getTimestamp("CreatedAt").toLocalDateTime()), storeId.toString());
    }

    @Transactional
    public void addProduct(Store store, ProductForm form, String imageUrl) {
        String medicineId = UUID.randomUUID().toString();
        jdbc.update("""
                INSERT INTO Medicines (Id, Name, Category, Description, Price, StockQuantity, ImageUrl, IsActive, CreatedAt)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, UTC_TIMESTAMP(6))
                """, medicineId, form.getName().trim(), form.getCategory().trim(), form.getDescription().trim(),
                form.getPrice(), form.getStockQuantity(), imageUrl, true);
        jdbc.update("""
                INSERT INTO StoreInventories (Id, StoreId, MedicineId, CreatedAt)
                VALUES (?, ?, ?, UTC_TIMESTAMP(6))
                """, UUID.randomUUID().toString(), store.getId().toString(), medicineId);
    }

    @Transactional
    public void updateProduct(Store store, UUID medicineId, ProductForm form, String imageUrl) {
        Integer owned = jdbc.queryForObject(
                "SELECT COUNT(*) FROM StoreInventories WHERE StoreId = ? AND MedicineId = ?",
                Integer.class, store.getId().toString(), medicineId.toString());
        if (owned == null || owned == 0) return;

        if (imageUrl == null) {
            jdbc.update("UPDATE Medicines SET Name = ?, Category = ?, Description = ?, Price = ?, StockQuantity = ? WHERE Id = ?",
                    form.getName().trim(), form.getCategory().trim(), form.getDescription().trim(), form.getPrice(), form.getStockQuantity(), medicineId.toString());
        } else {
            jdbc.update("UPDATE Medicines SET Name = ?, Category = ?, Description = ?, Price = ?, StockQuantity = ?, ImageUrl = ? WHERE Id = ?",
                    form.getName().trim(), form.getCategory().trim(), form.getDescription().trim(), form.getPrice(), form.getStockQuantity(), imageUrl, medicineId.toString());
        }
    }
}
