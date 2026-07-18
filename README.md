# Sublimation Clothing Inventory System

A desktop **inventory management** app (VB.NET WinForms) for a small sublimation-printing
clothing business — tracking blanks, ink, transfer paper, mug blanks, printed garments, etc.

> Inventory only — no purchase orders, payments, approvals or multi-user roles (out of scope by design).

## Requirements
- Windows + **Visual Studio 2022**
- **.NET Framework 4.8** (targeted by the project)
- **SQL Server LocalDB** — ships with Visual Studio
  (*Individual components → SQL Server Express 2019 LocalDB* if it isn't already installed)

## Run it
1. `git clone` the repo (or unzip).
2. Open **`SublimationInventory.sln`** in Visual Studio 2022.
3. Press **F5**.

On first launch the app automatically:
- creates the `SublimationInventory` LocalDB database,
- creates the tables,
- seeds 1 admin user, 5 sample items and 4 sample transactions.

No manual database setup is required.

### Default login
```
username: admin
password: admin123
```
The password is stored as a salted SHA-256 hash. To reset everything, delete the
`SublimationInventory` database from *View → SQL Server Object Explorer → (localdb)\MSSQLLocalDB*
and re-run — it will be recreated and re-seeded.

## Screens
- **Login** — single admin account, hashed password.
- **Dashboard** — KPI cards (units in stock, low-stock count, today's movements, transactions
  this month), a 7-day stock-movement chart, and the 10 most recent transactions.
- **Stock In / Out** — two tabs; Stock Out is blocked if it would drive on-hand below zero.
  Filterable transaction history below.
- **Supplies** — CRUD for items; on-hand is **computed**, and rows at/below their reorder
  threshold are highlighted amber.
- **Reports** — low-stock, stock movement (with running on-hand), and stock-value summary;
  each exportable to CSV.

## How on-hand works (single source of truth)
On-hand is **never stored**. It is always computed as
`SUM(In.Quantity) − SUM(Out.Quantity)` per item in **`Services/InventoryService`**.
The Dashboard, Supplies highlighting and every Report call into that same service, so there
is no second calculation path to drift out of sync.

## Tech choices
- **.NET Framework 4.8** (safest "clone-and-build" default; switch to .NET 8 if your box is set up for it).
- **ADO.NET + `System.Data.SqlClient`** rather than EF — zero NuGet restore, builds out of the box.
- **LocalDB**, created/seeded programmatically on startup (no `.mdf` to ship or path-fix).
- Flat, custom-drawn GDI+ UI (maroon sidebar + amber accent); no default gray WinForms boxes.

## Project structure
```
/Forms      Login, Main shell, Dashboard, StockInOut, Supplies, Reports
/Models     Item, StockTransaction, User, ItemStock (view model)
/Data       Database (init/seed) + ADO.NET repositories
/Services   InventoryService (shared on-hand/low-stock), AuthService
/UI         Theme, custom NavButton/RoundedPanel, helpers (grid styling, CSV export)
```

## Schema
```
Users(UserId, Username, PasswordHash, FullName)
Items(ItemId, Name, Category, Unit, ReorderThreshold, UnitCost)
StockTransactions(TransactionId, ItemId, Type[In|Out], Quantity, TransactionDate,
                  Source [In only], Reason [Out only], Notes)
```
