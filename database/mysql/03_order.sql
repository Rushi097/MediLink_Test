CREATE DATABASE IF NOT EXISTS `MediLinkOrder` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `MediLinkOrder`;

CREATE TABLE IF NOT EXISTS `Carts` (
  `Id` char(36) NOT NULL,
  `UserId` char(36) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Carts_UserId` (`UserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `CartItems` (
  `Id` char(36) NOT NULL,
  `CartId` char(36) NOT NULL,
  `MedicineId` char(36) NOT NULL,
  `StoreId` char(36) NOT NULL,
  `MedicineName` longtext NOT NULL,
  `UnitPrice` decimal(10,2) NOT NULL,
  `ImageUrl` longtext NULL,
  `Quantity` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_CartItems_CartId` (`CartId`),
  CONSTRAINT `FK_CartItems_Carts_CartId` FOREIGN KEY (`CartId`) REFERENCES `Carts` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Orders` (
  `Id` char(36) NOT NULL,
  `UserId` char(36) NOT NULL,
  `DeliveryAddress` longtext NOT NULL,
  `TotalAmount` decimal(10,2) NOT NULL,
  `Status` int NOT NULL,
  `PaymentMethod` varchar(30) NOT NULL DEFAULT 'CashOnDelivery',
  `ReservationId` char(36) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Orders_UserId` (`UserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `OrderItems` (
  `Id` char(36) NOT NULL,
  `OrderId` char(36) NOT NULL,
  `MedicineId` char(36) NOT NULL,
  `StoreId` char(36) NOT NULL,
  `MedicineName` longtext NOT NULL,
  `UnitPrice` decimal(10,2) NOT NULL,
  `Quantity` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_OrderItems_OrderId` (`OrderId`),
  CONSTRAINT `FK_OrderItems_Orders_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `StoreOrderAssignments` (
  `Id` char(36) NOT NULL,
  `StoreId` char(36) NOT NULL,
  `OrderId` char(36) NOT NULL,
  `StoreName` varchar(200) NOT NULL DEFAULT '',
  `StoreAddress` varchar(500) NOT NULL DEFAULT '',
  `AssignedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_StoreOrderAssignments_OrderId` (`OrderId`),
  KEY `IX_StoreOrderAssignments_StoreId` (`StoreId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
