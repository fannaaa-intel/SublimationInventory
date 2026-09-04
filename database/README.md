# Database

MySQL / MariaDB (XAMPP). Database name: `sublimationinventory`.

## Which file do I use?

| File | Use it? | What it contains |
|---|---|---|
| `backup_2026-09-04.sql` | **Yes — moving machines or restoring** | Full dump **with data**: 21 items + the `Inventory` login. The only file here that carries actual rows. Import into an empty `sublimationinventory` database. |
| `insert_items.sql` | Only to add the 21 starter supplies | `INSERT`s for the item list, nothing else. Running it twice duplicates rows. |
| `rebuild_sublimationinventory.sql` | **After a crash / corruption** | Same schema, but **drops** the three tables first and seeds a working `Inventory` / `1234` login, so the app is usable straight from SQL with no first run needed. Destroys existing rows — read the warning at the top. |
| `sublimationinventory.sql` | **Yes, for a normal setup** | Full schema: `users`, `items`, `stocktransactions`, keys and the foreign key. Creates nothing if the tables already exist, and seeds no user. |
| `items.sql` | No | The original phpMyAdmin export. **Only** the `items` table. Kept for reference. |

Importing `items.sql` on its own leaves you unable to sign in (no `users`
table) and every screen erroring (no `stocktransactions` table).

## Setting up

Easiest: **just run the application.** On first start it creates the
database, the tables and the default account by itself — see
`Data/Database.vb` → `Initialize()`. Nothing needs importing.

To create it by hand instead, start MySQL in the XAMPP Control Panel and
import `sublimationinventory.sql` in phpMyAdmin.

## Moving the system to another computer

`git clone` copies the **code and these .sql files** — it does not copy
the database itself. MySQL keeps its data in `C:\xampp\mysql\data`,
which is outside the repo. So on the new machine you must import the
data once:

1. Install XAMPP, start **MySQL** in the Control Panel.
2. phpMyAdmin → **New** → create a database named exactly
   `sublimationinventory` (collation `utf8mb4_general_ci`).
3. Select it → **Import** → choose `backup_2026-09-04.sql` → **Go**.
4. Open the solution in Visual Studio and run it.
5. Sign in: `Inventory` / `1234`.

Command line instead of steps 2–3:

```
C:\xampp\mysql\bin\mysql.exe -u root -e "CREATE DATABASE sublimationinventory"
C:\xampp\mysql\bin\mysql.exe -u root sublimationinventory < database\backup_2026-09-04.sql
```

**Before you clone, export a fresh backup** from the old machine
(phpMyAdmin → Export → **Custom** → *Structure and data*) and commit it,
or the new laptop gets whatever was in the last committed dump. The
export must include data — a "Structure only" export has no `INSERT`
lines and restores an empty system.

Also note `bin/`, `obj/` and `packages/` are gitignored, so let Visual
Studio restore the NuGet packages and rebuild on the new machine.

## Recovering from a corrupted or missing database

Run `rebuild_sublimationinventory.sql` in phpMyAdmin (SQL tab → paste → Go)
or:

```
C:\xampp\mysql\bin\mysql.exe -u root < rebuild_sublimationinventory.sql
```

It drops and recreates the tables, so **export anything still readable
first**. It also inserts the default account directly, so you can sign in
without running the app first.

## Backups

There was no backup when the database was lost. One command prevents a
repeat:

```
C:\xampp\mysql\bin\mysqldump.exe -u root sublimationinventory > backup_YYYY-MM-DD.sql
```

Keep copies off this machine, and always stop MySQL from the XAMPP Control
Panel before shutting Windows down — killing the server mid-write is the
usual cause of InnoDB corruption.

## Default account

Seeded by the app on first run, not by the SQL script — passwords are
salted per user, so a hash cannot be hard-coded in a dump.

```
username: Inventory
password: 1234
```

Change it before real use.

## Connection settings

`App.config`, connection string `InventoryDb`. Defaults to
`localhost:3306`, user `root`, empty password — the usual XAMPP setup.

If startup fails with *access denied* using `caching_sha2_password`, a
standalone MySQL 8 server is probably holding port 3306 instead of XAMPP's
MariaDB. Stop it, or point `App.config` at the right port.

## Stock levels are computed, never stored

There is no on-hand column anywhere. Stock is always derived from
`stocktransactions` (sum of `In` minus sum of `Out`) by
`Services/InventoryService.vb`. Dashboard, Supplies and Reports all read
from that one place, so they cannot disagree.

## Note on `users.ProfileImage`

Older databases have a `ProfileImage` column. The sidebar shows the company
logo now rather than a per-user photo, so nothing reads or writes it. It is
harmless. To reclaim the space — this permanently deletes any stored
pictures, so back up first:

```sql
ALTER TABLE `users` DROP COLUMN `ProfileImage`;
```
