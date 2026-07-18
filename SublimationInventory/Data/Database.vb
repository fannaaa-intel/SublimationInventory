Imports System.Configuration
Imports MySql.Data.MySqlClient
Imports SublimationInventory.Services

Namespace Data
    ''' <summary>Connection handling + first-run DB creation, schema and seed data. (XAMPP MySQL)</summary>
    Public Module Database

        Private Const DbName As String = "sublimationinventory"

        ''' <summary>Set to True to insert the five demo items + sample transactions on a fresh database.</summary>
        Private Const SeedSampleData As Boolean = False

        ''' <summary>Server-only connection (no database selected) - used to CREATE DATABASE.</summary>
        Private ReadOnly Property ServerConnectionString As String
            Get
                Dim cs = ConfigurationManager.ConnectionStrings("InventoryServer")
                If cs IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(cs.ConnectionString) Then
                    Return cs.ConnectionString
                End If
                Return "Server=localhost;Port=3306;Uid=root;Pwd=;SslMode=None;"
            End Get
        End Property

        Public ReadOnly Property ConnectionString As String
            Get
                Dim cs = ConfigurationManager.ConnectionStrings("InventoryDb")
                If cs IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(cs.ConnectionString) Then
                    Return cs.ConnectionString
                End If
                Return "Server=localhost;Port=3306;Database=" & DbName & ";Uid=root;Pwd=;SslMode=None;"
            End Get
        End Property

        Public Function GetConnection() As MySqlConnection
            Return New MySqlConnection(ConnectionString)
        End Function

        Public Sub Initialize()
            EnsureDatabase()
            EnsureSchema()
            Seed()
        End Sub

        Private Sub EnsureDatabase()
            Using conn As New MySqlConnection(ServerConnectionString)
                conn.Open()
                Using cmd As New MySqlCommand(
                    "CREATE DATABASE IF NOT EXISTS `" & DbName & "` " &
                    "CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;", conn)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Private Sub EnsureSchema()
            Dim usersDdl =
"CREATE TABLE IF NOT EXISTS Users(
    UserId INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(50) NOT NULL,
    PasswordHash VARCHAR(200) NOT NULL,
    FullName VARCHAR(100) NULL
) ENGINE=InnoDB;"

            Dim itemsDdl =
"CREATE TABLE IF NOT EXISTS Items(
    ItemId INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Category VARCHAR(50) NULL,
    Unit VARCHAR(20) NULL,
    ReorderThreshold INT NOT NULL DEFAULT 0,
    UnitCost DECIMAL(18,2) NOT NULL DEFAULT 0
) ENGINE=InnoDB;"

            Dim txDdl =
"CREATE TABLE IF NOT EXISTS StockTransactions(
    TransactionId INT AUTO_INCREMENT PRIMARY KEY,
    ItemId INT NOT NULL,
    `Type` VARCHAR(3) NOT NULL,
    Quantity INT NOT NULL,
    TransactionDate DATETIME NOT NULL,
    Source VARCHAR(200) NULL,
    Reason VARCHAR(200) NULL,
    Notes VARCHAR(500) NULL,
    CONSTRAINT FK_Tx_Item FOREIGN KEY (ItemId) REFERENCES Items(ItemId)
) ENGINE=InnoDB;"

            Using conn = GetConnection()
                conn.Open()
                Exec(conn, usersDdl)
                Exec(conn, itemsDdl)
                Exec(conn, txDdl)
                EnsureUserImageColumn(conn)
            End Using
        End Sub

        ''' <summary>Adds Users.ProfileImage if it isn't there yet (safe on existing databases).</summary>
        Private Sub EnsureUserImageColumn(conn As MySqlConnection)
            Dim check = "SELECT COUNT(*) FROM information_schema.COLUMNS " &
                        "WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Users' AND COLUMN_NAME='ProfileImage'"
            Using cmd As New MySqlCommand(check, conn)
                If Convert.ToInt32(cmd.ExecuteScalar()) = 0 Then
                    Exec(conn, "ALTER TABLE Users ADD COLUMN ProfileImage LONGBLOB NULL")
                End If
            End Using
        End Sub

        Private Sub Exec(conn As MySqlConnection, sql As String)
            Using cmd As New MySqlCommand(sql, conn)
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Sub Seed()
            Using conn = GetConnection()
                conn.Open()

                ' --- Admin user ---
                If Convert.ToInt32(Scalar(conn, "SELECT COUNT(*) FROM Users")) = 0 Then
                    Using cmd As New MySqlCommand("INSERT INTO Users(Username,PasswordHash,FullName) VALUES(@u,@h,@f)", conn)
                        cmd.Parameters.AddWithValue("@u", "Inventory")
                        cmd.Parameters.AddWithValue("@h", AuthService.HashPassword("1234"))
                        cmd.Parameters.AddWithValue("@f", "Inventory")
                        cmd.ExecuteNonQuery()
                    End Using
                End If

                ' --- Sample items + transactions ---
                ' Disabled: the system now starts empty. Flip SeedSampleData to True to restore demo data.
                If SeedSampleData AndAlso Convert.ToInt32(Scalar(conn, "SELECT COUNT(*) FROM Items")) = 0 Then
                    InsertItem(conn, "Gildan White T-Shirt (M)", "Blank Shirt", "pcs", 20, 3.5D)
                    InsertItem(conn, "Sublimation Ink - Cyan", "Ink", "liter", 2, 28D)
                    InsertItem(conn, "Transfer Paper A4", "Transfer Paper", "roll", 5, 15D)
                    InsertItem(conn, "11oz Mug Blank", "Mug Blank", "pcs", 24, 1.8D)
                    InsertItem(conn, "Polyester Tote Bag", "Blank Bag", "pcs", 15, 2.25D)

                    Dim shirtId = Convert.ToInt32(Scalar(conn, "SELECT ItemId FROM Items WHERE Name LIKE 'Gildan%' LIMIT 1"))
                    Dim mugId = Convert.ToInt32(Scalar(conn, "SELECT ItemId FROM Items WHERE Name LIKE '11oz%' LIMIT 1"))

                    InsertTx(conn, shirtId, "In", 100, DateTime.Now.AddDays(-6), "Blank Apparel Co.", Nothing, "Opening stock delivery")
                    InsertTx(conn, mugId, "In", 48, DateTime.Now.AddDays(-4), "MugSource Ltd.", Nothing, "Restock")
                    InsertTx(conn, shirtId, "Out", 12, DateTime.Now.AddDays(-2), Nothing, "Used in order", "Order #1043")
                    InsertTx(conn, mugId, "Out", 6, DateTime.Now.AddDays(-1), Nothing, "Sample", "Trade show samples")
                End If
            End Using
        End Sub

        Private Sub InsertItem(conn As MySqlConnection, name As String, cat As String, unit As String, reorder As Integer, cost As Decimal)
            Using cmd As New MySqlCommand("INSERT INTO Items(Name,Category,Unit,ReorderThreshold,UnitCost) VALUES(@n,@c,@u,@r,@co)", conn)
                cmd.Parameters.AddWithValue("@n", name)
                cmd.Parameters.AddWithValue("@c", cat)
                cmd.Parameters.AddWithValue("@u", unit)
                cmd.Parameters.AddWithValue("@r", reorder)
                cmd.Parameters.AddWithValue("@co", cost)
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Sub InsertTx(conn As MySqlConnection, itemId As Integer, type As String, qty As Integer, dt As DateTime, source As String, reason As String, notes As String)
            Using cmd As New MySqlCommand("INSERT INTO StockTransactions(ItemId,`Type`,Quantity,TransactionDate,Source,Reason,Notes) VALUES(@i,@t,@q,@d,@s,@r,@n)", conn)
                cmd.Parameters.AddWithValue("@i", itemId)
                cmd.Parameters.AddWithValue("@t", type)
                cmd.Parameters.AddWithValue("@q", qty)
                cmd.Parameters.AddWithValue("@d", dt)
                cmd.Parameters.AddWithValue("@s", If(source, CObj(DBNull.Value)))
                cmd.Parameters.AddWithValue("@r", If(reason, CObj(DBNull.Value)))
                cmd.Parameters.AddWithValue("@n", If(notes, CObj(DBNull.Value)))
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Function Scalar(conn As MySqlConnection, sql As String) As Object
            Using cmd As New MySqlCommand(sql, conn)
                Return cmd.ExecuteScalar()
            End Using
        End Function
    End Module
End Namespace