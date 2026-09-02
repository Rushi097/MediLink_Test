-- MediLink database-per-service databases.
-- The Windows launcher normally creates these automatically through EF Core.
-- Run this only if you want to create the databases manually in MySQL.

CREATE DATABASE IF NOT EXISTS `MediLinkAuth`
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Inventory is flat-file backed in the current version; no MediLinkInventory database is required.

CREATE DATABASE IF NOT EXISTS `MediLinkOrder`
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
