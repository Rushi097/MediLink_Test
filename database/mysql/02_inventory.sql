-- LEGACY: Inventory no longer uses MySQL in the flat-file inventory version.
-- Retained for migration/reference only. See ../FLAT_FILE_INVENTORY.md.
CREATE DATABASE IF NOT EXISTS `MediLinkInventory` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `MediLinkInventory`;

-- Medicine master references are intentionally lightweight. Authoritative metadata is
-- resolved from RxNorm/DailyMed by the Inventory Medicine Catalog API.
CREATE TABLE IF NOT EXISTS `Medicines` (
  `Id` char(36) NOT NULL,
  `ExternalMedicineId` varchar(100) NULL,
  `ExternalSource` varchar(40) NULL,
  `Name` longtext NOT NULL,
  `Description` longtext NOT NULL,
  `Category` longtext NOT NULL,
  `Price` decimal(10,2) NOT NULL DEFAULT 0,
  `StockQuantity` int NOT NULL DEFAULT 0,
  `ImageUrl` longtext NULL,
  `IsActive` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Medicines_ExternalMedicineId` (`ExternalMedicineId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Stores` (
  `Id` char(36) NOT NULL,
  `Name` longtext NOT NULL,
  `Address` longtext NOT NULL,
  `StoreOwnerProfileId` char(36) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Stores_StoreOwnerProfileId` (`StoreOwnerProfileId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `StoreInventories` (
  `Id` char(36) NOT NULL,
  `StoreId` char(36) NOT NULL,
  `MedicineId` char(36) NOT NULL,
  `Price` decimal(10,2) NOT NULL DEFAULT 0,
  `StockQuantity` int NOT NULL DEFAULT 0,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_StoreInventories_Store_Medicine` (`StoreId`,`MedicineId`),
  KEY `IX_StoreInventories_MedicineId` (`MedicineId`),
  CONSTRAINT `FK_StoreInventories_Stores_StoreId` FOREIGN KEY (`StoreId`) REFERENCES `Stores` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_StoreInventories_Medicines_MedicineId` FOREIGN KEY (`MedicineId`) REFERENCES `Medicines` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
