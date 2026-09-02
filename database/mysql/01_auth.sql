CREATE DATABASE IF NOT EXISTS `MediLinkAuth` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `MediLinkAuth`;
CREATE TABLE IF NOT EXISTS `Users` (
  `Id` char(36) NOT NULL,
  `Email` varchar(255) NOT NULL,
  `PasswordHash` longtext NOT NULL,
  `FirstName` longtext NOT NULL,
  `LastName` longtext NOT NULL,
  `Role` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`), UNIQUE KEY `IX_Users_Email` (`Email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE TABLE IF NOT EXISTS `CustomerProfiles` (
  `Id` char(36) NOT NULL, `UserId` char(36) NOT NULL,
  `DeliveryAddress` longtext NOT NULL, `PhoneNumber` longtext NOT NULL,
  PRIMARY KEY (`Id`), UNIQUE KEY `IX_CustomerProfiles_UserId` (`UserId`),
  CONSTRAINT `FK_CustomerProfiles_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE TABLE IF NOT EXISTS `StoreOwnerProfiles` (
  `Id` char(36) NOT NULL, `UserId` char(36) NOT NULL,
  `BusinessLicenseNumber` longtext NOT NULL,
  PRIMARY KEY (`Id`), UNIQUE KEY `IX_StoreOwnerProfiles_UserId` (`UserId`),
  CONSTRAINT `FK_StoreOwnerProfiles_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
