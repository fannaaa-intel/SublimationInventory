-- =====================================================================
-- SUBLIMATION INVENTORY - INSERT STARTER ITEMS
--
-- Adds supply rows to the `items` table of an existing database.
--
-- HOW TO RUN
--   phpMyAdmin  : SQL tab -> paste -> Go
--   Command line: mysql -u root < insert_items.sql
--
-- Safe to run on a database that already has data: it only INSERTs,
-- it never drops or empties anything. Running it twice will create
-- duplicate rows, so run it once.
--
-- NOTE: the phpMyAdmin file `items.sql` contains NO item data - it is a
-- "Structure only" export (a CREATE TABLE, no INSERTs), so importing it
-- adds no products. This file supplies the rows instead.
--
-- Columns match Data\ItemRepository.vb exactly. ItemId is auto_increment
-- and is deliberately not listed, so MySQL assigns the ids.
--
-- ReorderThreshold drives the low-stock alerts. An item left at 0 is
-- never reported as low (see Models\ItemStock.vb), so set a real number
-- on anything you want warnings for.
--
-- UnitCost is the cost per single unit, used for Stock Value reports.
-- ADJUST THE PRICES BELOW TO YOUR ACTUAL COSTS.
-- =====================================================================

USE `sublimationinventory`;

INSERT INTO `items` (`Name`, `Category`, `Unit`, `ReorderThreshold`, `UnitCost`) VALUES
-- Blank apparel
('Gildan White T-Shirt (S)',      'Blank Shirt',    'pcs',   20, 3.50),
('Gildan White T-Shirt (M)',      'Blank Shirt',    'pcs',   20, 3.50),
('Gildan White T-Shirt (L)',      'Blank Shirt',    'pcs',   20, 3.50),
('Gildan White T-Shirt (XL)',     'Blank Shirt',    'pcs',   15, 3.75),
('Polyester Jersey (M)',          'Blank Shirt',    'pcs',   10, 5.00),
('Polyester Jersey (L)',          'Blank Shirt',    'pcs',   10, 5.00),

-- Sublimation ink
('Sublimation Ink - Cyan',        'Ink',            'liter',  2, 28.00),
('Sublimation Ink - Magenta',     'Ink',            'liter',  2, 28.00),
('Sublimation Ink - Yellow',      'Ink',            'liter',  2, 28.00),
('Sublimation Ink - Black',       'Ink',            'liter',  2, 28.00),

-- Paper and film
('Transfer Paper A4',             'Transfer Paper', 'roll',   5, 15.00),
('Transfer Paper A3',             'Transfer Paper', 'roll',   5, 22.00),
('Heat Transfer Vinyl - White',   'Vinyl',          'roll',   3, 18.00),
('Heat Transfer Vinyl - Black',   'Vinyl',          'roll',   3, 18.00),

-- Hard goods
('11oz Mug Blank',                'Mug Blank',      'pcs',   24, 1.80),
('15oz Mug Blank',                'Mug Blank',      'pcs',   12, 2.40),
('Polyester Tote Bag',            'Blank Bag',      'pcs',   15, 2.25),
('Mouse Pad Blank',               'Blank Other',    'pcs',   10, 1.50),

-- Consumables
('Heat Resistant Tape',           'Consumable',     'roll',   6, 2.00),
('Butcher Paper',                 'Consumable',     'roll',   4, 8.00),
('Teflon Sheet',                  'Consumable',     'pcs',    2, 6.50);

-- =====================================================================
-- VERIFY
--
--   SELECT ItemId, Name, Category, Unit, ReorderThreshold, UnitCost
--   FROM items ORDER BY Category, Name;
--
-- =====================================================================
-- NEXT STEP - PUT STOCK AGAINST THEM
--
-- These rows describe WHAT you stock, not HOW MUCH. On-hand quantity is
-- never stored in `items`; it is always computed from stocktransactions
-- (SUM of In minus SUM of Out) by Services\InventoryService.vb. Until
-- you record movements, every item shows 0 on hand.
--
-- Easiest way: use the Stock In/Out screen in the app.
--
-- Or record an opening balance for everything at once - edit the
-- quantities, then run:
--
--   INSERT INTO stocktransactions (ItemId, `Type`, Quantity, TransactionDate, Source, Notes)
--   SELECT ItemId, 'In', 100, NOW(), 'Physical count', 'Opening balance'
--   FROM items WHERE Name = 'Gildan White T-Shirt (M)';
--
-- Repeat per item with its real shelf quantity. `Type` must be exactly
-- 'In' or 'Out', and Quantity must always be POSITIVE - the direction
-- comes from Type, so a negative Out quantity would add stock back.
-- =====================================================================
