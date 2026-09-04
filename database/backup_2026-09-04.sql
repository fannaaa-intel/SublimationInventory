-- =====================================================================
-- FULL BACKUP - sublimationinventory - taken 2026-09-04
--
-- phpMyAdmin export WITH DATA (structure + rows). This is the file to
-- import when setting the system up on another machine.
--
--   21 items, 1 user (Inventory / 1234), 0 stock movements.
--
-- HOW TO RESTORE ON A NEW MACHINE
--   1. Start MySQL in the XAMPP Control Panel.
--   2. phpMyAdmin -> New -> create a database named exactly:
--          sublimationinventory
--      (collation utf8mb4_general_ci)
--   3. Select it -> Import -> choose this file -> Go.
--
--   Command line equivalent:
--      mysql -u root -e "CREATE DATABASE sublimationinventory"
--      mysql -u root sublimationinventory < backup_2026-09-04.sql
--
-- This dump does NOT contain a CREATE DATABASE statement, so the empty
-- database must exist first - that is what step 2 is for.
--
-- Sign in afterwards with:  Inventory / 1234
-- =====================================================================

-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: Sep 04, 2026 at 03:36 PM
-- Server version: 10.4.32-MariaDB
-- PHP Version: 8.0.30

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `sublimationinventory`
--

-- --------------------------------------------------------

--
-- Table structure for table `items`
--

CREATE TABLE `items` (
  `ItemId` int(11) NOT NULL,
  `Name` varchar(100) NOT NULL,
  `Category` varchar(50) DEFAULT NULL,
  `Unit` varchar(20) DEFAULT NULL,
  `ReorderThreshold` int(11) NOT NULL DEFAULT 0,
  `UnitCost` decimal(18,2) NOT NULL DEFAULT 0.00
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `items`
--

INSERT INTO `items` (`ItemId`, `Name`, `Category`, `Unit`, `ReorderThreshold`, `UnitCost`) VALUES
(1, 'Gildan White T-Shirt (S)', 'Blank Shirt', 'pcs', 20, 3.50),
(2, 'Gildan White T-Shirt (M)', 'Blank Shirt', 'pcs', 20, 3.50),
(3, 'Gildan White T-Shirt (L)', 'Blank Shirt', 'pcs', 20, 3.50),
(4, 'Gildan White T-Shirt (XL)', 'Blank Shirt', 'pcs', 15, 3.75),
(5, 'Polyester Jersey (M)', 'Blank Shirt', 'pcs', 10, 5.00),
(6, 'Polyester Jersey (L)', 'Blank Shirt', 'pcs', 10, 5.00),
(7, 'Sublimation Ink - Cyan', 'Ink', 'liter', 2, 28.00),
(8, 'Sublimation Ink - Magenta', 'Ink', 'liter', 2, 28.00),
(9, 'Sublimation Ink - Yellow', 'Ink', 'liter', 2, 28.00),
(10, 'Sublimation Ink - Black', 'Ink', 'liter', 2, 28.00),
(11, 'Transfer Paper A4', 'Transfer Paper', 'roll', 5, 15.00),
(12, 'Transfer Paper A3', 'Transfer Paper', 'roll', 5, 22.00),
(13, 'Heat Transfer Vinyl - White', 'Vinyl', 'roll', 3, 18.00),
(14, 'Heat Transfer Vinyl - Black', 'Vinyl', 'roll', 3, 18.00),
(15, '11oz Mug Blank', 'Mug Blank', 'pcs', 24, 1.80),
(16, '15oz Mug Blank', 'Mug Blank', 'pcs', 12, 2.40),
(17, 'Polyester Tote Bag', 'Blank Bag', 'pcs', 15, 2.25),
(18, 'Mouse Pad Blank', 'Blank Other', 'pcs', 10, 1.50),
(19, 'Heat Resistant Tape', 'Consumable', 'roll', 6, 2.00),
(20, 'Butcher Paper', 'Consumable', 'roll', 4, 8.00),
(21, 'Teflon Sheet', 'Consumable', 'pcs', 2, 6.50);

-- --------------------------------------------------------

--
-- Table structure for table `stocktransactions`
--

CREATE TABLE `stocktransactions` (
  `TransactionId` int(11) NOT NULL,
  `ItemId` int(11) NOT NULL,
  `Type` varchar(3) NOT NULL,
  `Quantity` int(11) NOT NULL,
  `TransactionDate` datetime NOT NULL,
  `Source` varchar(200) DEFAULT NULL,
  `Reason` varchar(200) DEFAULT NULL,
  `Notes` varchar(500) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `users`
--

CREATE TABLE `users` (
  `UserId` int(11) NOT NULL,
  `Username` varchar(50) NOT NULL,
  `PasswordHash` varchar(200) NOT NULL,
  `FullName` varchar(100) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `users`
--

INSERT INTO `users` (`UserId`, `Username`, `PasswordHash`, `FullName`) VALUES
(1, 'Inventory', 'z1OM/vp5NRX4c9D9PUahcw==:XCh2FlPJ74gUxZDUe3g7QUs9Eb+UKGrglDWjgueMZJM=', 'Inventory');

--
-- Indexes for dumped tables
--

--
-- Indexes for table `items`
--
ALTER TABLE `items`
  ADD PRIMARY KEY (`ItemId`),
  ADD KEY `IX_Items_Name` (`Name`);

--
-- Indexes for table `stocktransactions`
--
ALTER TABLE `stocktransactions`
  ADD PRIMARY KEY (`TransactionId`),
  ADD KEY `IX_Tx_ItemId` (`ItemId`),
  ADD KEY `IX_Tx_Date` (`TransactionDate`);

--
-- Indexes for table `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`UserId`),
  ADD UNIQUE KEY `UQ_Users_Username` (`Username`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `items`
--
ALTER TABLE `items`
  MODIFY `ItemId` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=22;

--
-- AUTO_INCREMENT for table `stocktransactions`
--
ALTER TABLE `stocktransactions`
  MODIFY `TransactionId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `users`
--
ALTER TABLE `users`
  MODIFY `UserId` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `stocktransactions`
--
ALTER TABLE `stocktransactions`
  ADD CONSTRAINT `FK_Tx_Item` FOREIGN KEY (`ItemId`) REFERENCES `items` (`ItemId`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
