-- Optional one-time migration from the former shared MediLink database.
-- The new runtime does not require this file. It is intentionally conservative:
-- user/store/order records can be migrated, but legacy medicine rows are not treated
-- as authoritative external catalogue records.
INSERT IGNORE INTO MediLinkAuth.Users SELECT * FROM MediLink.Users;
INSERT IGNORE INTO MediLinkAuth.CustomerProfiles SELECT * FROM MediLink.CustomerProfiles;
INSERT IGNORE INTO MediLinkAuth.StoreOwnerProfiles SELECT * FROM MediLink.StoreOwnerProfiles;

-- Legacy medicines are copied only as references. They will not appear in the new
-- customer catalogue until they are linked to an ExternalMedicineId by the new flow.
INSERT IGNORE INTO MediLinkInventory.Medicines
  (Id, Name, Description, Category, Price, StockQuantity, ImageUrl, IsActive, CreatedAt)
SELECT Id, Name, Description, Category, Price, StockQuantity, ImageUrl, IsActive, CreatedAt
FROM MediLink.Medicines;

INSERT IGNORE INTO MediLinkInventory.Stores SELECT * FROM MediLink.Stores;

INSERT IGNORE INTO MediLinkInventory.StoreInventories
  (Id, StoreId, MedicineId, Price, StockQuantity, CreatedAt)
SELECT si.Id, si.StoreId, si.MedicineId, COALESCE(m.Price, 0), COALESCE(m.StockQuantity, 0), si.CreatedAt
FROM MediLink.StoreInventories si
LEFT JOIN MediLink.Medicines m ON m.Id = si.MedicineId;

INSERT IGNORE INTO MediLinkOrder.Carts (Id, UserId)
SELECT Id, UserId FROM MediLink.Carts;

INSERT IGNORE INTO MediLinkOrder.CartItems
  (Id, CartId, MedicineId, StoreId, MedicineName, UnitPrice, ImageUrl, Quantity)
SELECT c.Id, c.CartId, c.MedicineId, si.StoreId,
       COALESCE(m.Name,''), COALESCE(si.Price, m.Price, 0), m.ImageUrl, c.Quantity
FROM MediLink.CartItems c
LEFT JOIN MediLink.Medicines m ON m.Id = c.MedicineId
LEFT JOIN MediLink.StoreInventories si ON si.MedicineId = c.MedicineId;

INSERT IGNORE INTO MediLinkOrder.Orders SELECT * FROM MediLink.Orders;

INSERT IGNORE INTO MediLinkOrder.OrderItems
  (Id, OrderId, MedicineId, StoreId, MedicineName, UnitPrice, Quantity)
SELECT oi.Id, oi.OrderId, oi.MedicineId,
       COALESCE(si.StoreId, '00000000-0000-0000-0000-000000000000'),
       oi.MedicineName, oi.UnitPrice, oi.Quantity
FROM MediLink.OrderItems oi
LEFT JOIN MediLink.StoreInventories si ON si.MedicineId = oi.MedicineId;

INSERT IGNORE INTO MediLinkOrder.StoreOrderAssignments
SELECT * FROM MediLink.StoreOrderAssignments;
