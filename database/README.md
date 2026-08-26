# Database

MySQL / MariaDB (XAMPP). Database name: `sublimationinventory`.

## Which file do I use?

| File | Use it? | What it contains |
|---|---|---|
| `sublimationinventory.sql` | **Yes** | Full schema: `users`, `items`, `stocktransactions`, keys and the foreign key. |
| `items.sql` | No | The original phpMyAdmin export. **Only** the `items` table. Kept for reference. |

Importing `items.sql` on its own leaves you unable to sign in (no `users`
table) and every screen erroring (no `stocktransactions` table).

## Setting up

Easiest: **just run the application.** On first start it creates the
database, the tables and the default account by itself — see
`Data/Database.vb` → `Initialize()`. Nothing needs importing.

To create it by hand instead, start MySQL in the XAMPP Control Panel and
import `sublimationinventory.sql` in phpMyAdmin.

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
