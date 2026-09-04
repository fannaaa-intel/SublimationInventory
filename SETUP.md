# Setting up on another computer

Follow these in order. Takes about 15 minutes, most of it installing
XAMPP and Visual Studio.

`git clone` brings the **code and the .sql files**. It does **not** bring
the database or the NuGet packages — MySQL stores its data in
`C:\xampp\mysql\data`, outside the repo, and `packages/` is gitignored.
Steps 3 and 4 put those back.

---

## 1. Install the two prerequisites

| Need | Where | Notes |
|---|---|---|
| **XAMPP** (MySQL/MariaDB) | apachefriends.org | Only MySQL is required; Apache is optional (it serves phpMyAdmin). |
| **Visual Studio 2022** | visualstudio.microsoft.com | Community is fine. In the installer tick the **.NET desktop development** workload — it includes .NET Framework 4.8. |

## 2. Clone the repository

```
git clone https://github.com/fannaaa-intel/SublimationInventory.git
cd SublimationInventory
```

## 3. Restore the database  ← the step people forget

1. Open the **XAMPP Control Panel** and press **Start** next to *MySQL*.
2. Go to <http://localhost/phpmyadmin>.
3. Left panel → **New**. Database name, spelled exactly:

   ```
   sublimationinventory
   ```

   Collation `utf8mb4_general_ci`. Click **Create**.
4. Select that database → **Import** tab → **Choose File** →
   `database\backup_2026-09-04.sql` → **Go**.

You should see 3 tables and 21 rows in `items`.

Command-line alternative:

```
C:\xampp\mysql\bin\mysql.exe -u root -e "CREATE DATABASE sublimationinventory CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci"
C:\xampp\mysql\bin\mysql.exe -u root sublimationinventory < database\backup_2026-09-04.sql
```

> Import the newest `backup_*.sql` in `database\` — if you exported a
> fresher one from the old laptop, use that instead.

## 4. Restore the NuGet packages  ← the other one

The project uses `packages.config`, so **the first build fails without
this**. In Visual Studio: open `SublimationInventory.sln`, then right-click
the solution in Solution Explorer → **Restore NuGet Packages**.

If you have `nuget.exe` on PATH, this does the same thing:

```
nuget restore SublimationInventory.sln
```

Skipping this gives dozens of errors like
`BC30002: Type 'MySqlConnection' is not defined` — that is a missing
package, not broken code.

## 5. Build and run

Press **F5** in Visual Studio (or Build → Build Solution, then run).

Sign in:

```
username: Inventory
password: 1234
```

Change that password once you are in.

---

## If something goes wrong

**`BC30002: Type 'MySqlConnection' is not defined`** (many errors)
NuGet packages not restored — go back to step 4.

**"Unable to connect to any of the specified MySQL hosts"**
MySQL is not running. Start it in the XAMPP Control Panel.

**"Access denied for user 'root'@'localhost'"**
Your MySQL root account has a password. Put it in
`SublimationInventory\App.config`, in **both** connection strings:

```xml
Server=localhost;Port=3306;Database=sublimationinventory;Uid=root;Pwd=YOUR_PASSWORD;SslMode=None;
```

**"Unknown database 'sublimationinventory'"**
Step 3 was skipped, or the name is misspelled. It must match exactly.

**Login rejects Inventory / 1234**
The `users` table imported without its row. Check:
`SELECT * FROM users;` — if empty, re-import the backup.

**Port 3306 already in use / access denied with `caching_sha2_password`**
A standalone MySQL 8 server is competing with XAMPP's MariaDB. Open
`services.msc`, set the **MySQL80** service to **Manual**, reboot, then
start MySQL from the XAMPP Control Panel. *(This is what destroyed the
database on the original machine — worth doing pre-emptively if you have
MySQL 8 installed.)*

---

## Keeping the two machines in sync

The database does **not** travel through git by itself. To move data
across, export it and commit the file:

1. phpMyAdmin → select `sublimationinventory` → **Export**
2. Method: **Custom** → Format-specific options: **Structure and data**
   *(the default "Quick" export is fine too — just never "Structure only",
   which produces a file with no `INSERT` lines and restores an empty
   system)*
3. Save as `database\backup_YYYY-MM-DD.sql`, then:

```
git add database\backup_YYYY-MM-DD.sql
git commit -m "Database backup YYYY-MM-DD"
git push
```

On the other laptop: `git pull`, then re-import (step 3).

Back up regularly — the original database was lost with no copy:

```
C:\xampp\mysql\bin\mysqldump.exe -u root sublimationinventory > backup_YYYY-MM-DD.sql
```

And always stop MySQL from the XAMPP Control Panel before shutting
Windows down.
