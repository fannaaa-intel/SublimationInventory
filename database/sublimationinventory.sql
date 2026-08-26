-- =====================================================================
-- Sublimation Inventory - full database schema (MySQL / MariaDB, XAMPP)
--
-- Import this in phpMyAdmin to create the database from scratch.
-- The application also creates this schema itself on first run
-- (see Data\Database.vb -> Initialize), so importing is optional.
--
-- NOTE: this replaces the older items.sql, which contained ONLY the
-- `items` table. That file was missing `users` and `stocktransactions`,
-- so a database imported from it could not run the application:
--   * login failed      - no users table
--   * every screen threw - no stocktransactions table
--   * avatars failed     - no users.ProfileImage column
-- =====================================================================

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";
START TRANSACTION;

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

CREATE DATABASE IF NOT EXISTS `sublimationinventory`
  DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE `sublimationinventory`;

-- ---------------------------------------------------------------------
-- Table `users`
-- PasswordHash stores "saltBase64:hashBase64" (salted SHA-256), so it
-- needs room for both parts - see Services\AuthService.vb.
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `users` (
  `UserId`       INT(11)      NOT NULL AUTO_INCREMENT,
  `Username`     VARCHAR(50)  NOT NULL,
  `PasswordHash` VARCHAR(200) NOT NULL,
  `FullName`     VARCHAR(100) DEFAULT NULL,
  `ProfileImage` LONGBLOB     DEFAULT NULL,
  PRIMARY KEY (`UserId`),
  UNIQUE KEY `UQ_Users_Username` (`Username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ---------------------------------------------------------------------
-- Table `items`
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `items` (
  `ItemId`           INT(11)        NOT NULL AUTO_INCREMENT,
  `Name`             VARCHAR(100)   NOT NULL,
  `Category`         VARCHAR(50)    DEFAULT NULL,
  `Unit`             VARCHAR(20)    DEFAULT NULL,
  `ReorderThreshold` INT(11)        NOT NULL DEFAULT 0,
  `UnitCost`         DECIMAL(18,2)  NOT NULL DEFAULT 0.00,
  PRIMARY KEY (`ItemId`),
  KEY `IX_Items_Name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ---------------------------------------------------------------------
-- Table `stocktransactions`
-- On-hand stock is NEVER stored - it is always derived from these rows
-- (SUM of In minus SUM of Out) by Services\InventoryService.vb.
--
-- The FK to items is what makes ItemRepository.Delete refuse to remove
-- an item that still has history.
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `stocktransactions` (
  `TransactionId`   INT(11)      NOT NULL AUTO_INCREMENT,
  `ItemId`          INT(11)      NOT NULL,
  `Type`            VARCHAR(3)   NOT NULL,
  `Quantity`        INT(11)      NOT NULL,
  `TransactionDate` DATETIME     NOT NULL,
  `Source`          VARCHAR(200) DEFAULT NULL,
  `Reason`          VARCHAR(200) DEFAULT NULL,
  `Notes`           VARCHAR(500) DEFAULT NULL,
  PRIMARY KEY (`TransactionId`),
  KEY `IX_Tx_ItemId` (`ItemId`),
  KEY `IX_Tx_Date` (`TransactionDate`),
  CONSTRAINT `FK_Tx_Item` FOREIGN KEY (`ItemId`)
    REFERENCES `items` (`ItemId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;

-- =====================================================================
-- No default user is inserted here on purpose.
--
-- Passwords are salted per-user at runtime, so a hash cannot be
-- hard-coded in a script. Start the application once against this
-- database and it seeds the default account itself:
--
--     username: Inventory
--     password: 1234
--
-- Change that password before real use.
-- =====================================================================
