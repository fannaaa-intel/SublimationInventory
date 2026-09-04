-- =====================================================================
-- SUBLIMATION INVENTORY (RJA Sportswear) - DATABASE REBUILD / RECOVERY
-- MySQL / MariaDB (XAMPP)
--
-- USE THIS WHEN: the `sublimationinventory` database is corrupted, is
-- missing tables, or will not open, and there is no backup to restore.
--
-- HOW TO RUN
--   phpMyAdmin  : SQL tab -> paste this whole file -> Go
--   Command line: mysql -u root -p < rebuild_sublimationinventory.sql
--
-- You do NOT need to create or select the database first - this script
-- does that itself.
--
-- ---------------------------------------------------------------------
-- !! READ THIS BEFORE RUNNING !!
--
-- This script DROPS the three application tables and recreates them
-- empty. Any rows that are still readable in the corrupted database
-- will be permanently destroyed.
--
-- If phpMyAdmin can still show ANY rows in `items` or `stocktransactions`,
-- export them first (Export -> Go). Recovering 60% of your data beats
-- starting from zero. See "SALVAGING DATA" at the end of this file.
-- ---------------------------------------------------------------------
--
-- The schema below is taken directly from the application source, so it
-- matches exactly what the code reads and writes:
--   Data\Database.vb              - EnsureSchema (table definitions)
--   Data\ItemRepository.vb        - items columns
--   Data\TransactionRepository.vb - stocktransactions columns
--   Data\UserRepository.vb        - users columns
--   Services\AuthService.vb       - password hash format
-- =====================================================================

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

CREATE DATABASE IF NOT EXISTS `sublimationinventory`
  DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE `sublimationinventory`;

-- Drop children before parents so the foreign key does not block us.
SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS `stocktransactions`;
DROP TABLE IF EXISTS `items`;
DROP TABLE IF EXISTS `users`;
SET FOREIGN_KEY_CHECKS = 1;

-- ---------------------------------------------------------------------
-- users
--
-- PasswordHash holds "saltBase64:hashBase64" (salted SHA-256) and is
-- about 69 characters; VARCHAR(200) leaves room to spare.
-- See Services\AuthService.vb.
--
-- Username is UNIQUE here. The app's own CREATE TABLE omits that index,
-- but AuthService looks a user up by name and uses the first match, so
-- duplicates would make sign-in ambiguous. Adding it is safe and matches
-- how the application actually behaves.
-- ---------------------------------------------------------------------
CREATE TABLE `users` (
  `UserId`       INT(11)      NOT NULL AUTO_INCREMENT,
  `Username`     VARCHAR(50)  NOT NULL,
  `PasswordHash` VARCHAR(200) NOT NULL,
  `FullName`     VARCHAR(100) DEFAULT NULL,
  PRIMARY KEY (`UserId`),
  UNIQUE KEY `UQ_Users_Username` (`Username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ---------------------------------------------------------------------
-- items
-- One row per supply (blank shirt, ink, transfer paper, mug, ...).
-- There is deliberately NO quantity column - see the note further down.
-- ---------------------------------------------------------------------
CREATE TABLE `items` (
  `ItemId`           INT(11)       NOT NULL AUTO_INCREMENT,
  `Name`             VARCHAR(100)  NOT NULL,
  `Category`         VARCHAR(50)   DEFAULT NULL,
  `Unit`             VARCHAR(20)   DEFAULT NULL,
  `ReorderThreshold` INT(11)       NOT NULL DEFAULT 0,
  `UnitCost`         DECIMAL(18,2) NOT NULL DEFAULT 0.00,
  PRIMARY KEY (`ItemId`),
  KEY `IX_Items_Name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ---------------------------------------------------------------------
-- stocktransactions
-- Every stock movement. `Type` is exactly 'In' or 'Out' (case matters -
-- InventoryService compares with `Type`='In').
--
-- Quantity is always stored POSITIVE; the direction comes from `Type`.
-- Storing a negative quantity on an 'Out' row would add stock back.
--
-- The foreign key is what makes ItemRepository.Delete refuse to remove
-- an item that still has history - keep it.
-- ---------------------------------------------------------------------
CREATE TABLE `stocktransactions` (
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

-- ---------------------------------------------------------------------
-- Default sign-in account
--
-- The hash below is a real salted SHA-256 value in the exact format
-- AuthService.VerifyPassword expects, generated for the password 1234
-- and checked against that routine. You can sign in immediately after
-- running this script:
--
--       username: Inventory
--       password: 1234
--
-- CHANGE THIS PASSWORD BEFORE REAL USE - the salt and hash are printed
-- in this file, so anyone holding a copy knows the credentials.
--
-- (Every account gets its own random salt, so this one line does not
-- weaken any other user created later through the application.)
-- ---------------------------------------------------------------------
INSERT INTO `users` (`Username`, `PasswordHash`, `FullName`) VALUES
('Inventory',
 'z1OM/vp5NRX4c9D9PUahcw==:XCh2FlPJ74gUxZDUe3g7QUs9Eb+UKGrglDWjgueMZJM=',
 'Inventory');

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;

-- =====================================================================
-- AFTER RUNNING - verify (optional)
--
--   USE sublimationinventory;
--   SHOW TABLES;                                  -- items, stocktransactions, users
--   SELECT UserId, Username, FullName FROM users; -- 1 row: Inventory
--
-- Then start the application and sign in as Inventory / 1234.
-- The inventory starts empty: add supplies under Supplies, then record
-- movements under Stock In/Out.
-- =====================================================================
--
-- WHY THERE IS NO "QUANTITY ON HAND" COLUMN
--
-- On-hand stock is never stored. It is computed every time from the
-- movement rows, by Services\InventoryService.vb:
--
--   SELECT ItemId,
--          SUM(CASE WHEN `Type`='In' THEN Quantity ELSE -Quantity END)
--   FROM stocktransactions GROUP BY ItemId;
--
-- Dashboard, Supplies and Reports all read that one calculation, so they
-- can never disagree with each other. If you re-enter historical data,
-- enter it as In/Out movements - do not add a quantity column.
-- =====================================================================
--
-- SALVAGING DATA FROM THE CORRUPTED DATABASE
--
-- If you managed to export any old rows before running this, re-import
-- them in this order (items first - the foreign key requires the parent
-- row to exist):
--
--   1. items
--   2. stocktransactions
--
-- Load an old export into a scratch database rather than over this one,
-- then copy across only the rows that look sane:
--
--   INSERT INTO sublimationinventory.items
--     (ItemId, Name, Category, Unit, ReorderThreshold, UnitCost)
--   SELECT ItemId, Name, Category, Unit, ReorderThreshold, UnitCost
--   FROM   old_broken_db.items
--   WHERE  Name IS NOT NULL AND Name <> '';
--
--   INSERT INTO sublimationinventory.stocktransactions
--     (ItemId, `Type`, Quantity, TransactionDate, Source, Reason, Notes)
--   SELECT t.ItemId, t.`Type`, t.Quantity, t.TransactionDate,
--          t.Source, t.Reason, t.Notes
--   FROM   old_broken_db.stocktransactions t
--   JOIN   sublimationinventory.items i ON i.ItemId = t.ItemId
--   WHERE  t.`Type` IN ('In','Out')
--     AND  t.Quantity > 0
--     AND  t.TransactionDate IS NOT NULL;
--
-- The JOIN drops movements whose item no longer exists (they would
-- violate the foreign key), and the WHERE filters the malformed rows
-- corruption typically leaves behind.
--
-- If instead you have NO usable old data, the fastest way to get real
-- stock levels back is one 'In' movement per supply for the quantity
-- physically on the shelf today, dated today, with a note such as
-- 'Opening balance after database rebuild'. Example:
--
--   INSERT INTO items (Name, Category, Unit, ReorderThreshold, UnitCost)
--   VALUES ('Gildan White T-Shirt (M)', 'Blank Shirt', 'pcs', 20, 3.50);
--
--   INSERT INTO stocktransactions
--     (ItemId, `Type`, Quantity, TransactionDate, Source, Notes)
--   VALUES (LAST_INSERT_ID(), 'In', 100, NOW(), 'Physical count',
--           'Opening balance after database rebuild');
-- =====================================================================
--
-- IF THE DATABASE CORRUPTS AGAIN - TAKE BACKUPS
--
-- One command, and it is the thing that was missing this time:
--
--   mysqldump -u root sublimationinventory > backup_2026-09-04.sql
--
-- In XAMPP, mysqldump lives in C:\xampp\mysql\bin. Run it weekly and
-- keep the copies somewhere other than the machine running MySQL.
-- Restore with:
--
--   mysql -u root sublimationinventory < backup_2026-09-04.sql
--
-- A common cause of this kind of corruption is Windows shutting down or
-- losing power while MySQL is running. Always stop MySQL from the XAMPP
-- Control Panel before shutting the PC down.
-- =====================================================================
