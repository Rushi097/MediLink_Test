-- Supporting tables owned by the store portal. The existing MediLink tables stay managed by EF Core.
CREATE TABLE IF NOT EXISTS `StoreInventories` (
  `Id` varchar(36) NOT NULL,
  `StoreId` varchar(36) NOT NULL,
  `MedicineId` varchar(36) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_StoreInventories_Store_Medicine` (`StoreId`, `MedicineId`)
);

CREATE TABLE IF NOT EXISTS `StoreOrderAssignments` (
  `Id` varchar(36) NOT NULL,
  `StoreId` varchar(36) NOT NULL,
  `OrderId` varchar(36) NOT NULL,
  `AssignedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_StoreOrderAssignments_Order` (`OrderId`),
  KEY `IX_StoreOrderAssignments_Store` (`StoreId`)
);
