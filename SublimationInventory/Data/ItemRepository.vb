Imports MySql.Data.MySqlClient
Imports SublimationInventory.Models

Namespace Data
    Public Module ItemRepository

        Public Function GetAll() As List(Of Item)
            Dim list As New List(Of Item)()
            Using conn = Database.GetConnection()
                conn.Open()
                Using cmd As New MySqlCommand("SELECT ItemId,Name,Category,Unit,ReorderThreshold,UnitCost FROM Items ORDER BY Name", conn)
                    Using r = cmd.ExecuteReader()
                        While r.Read()
                            list.Add(Map(r))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Function GetById(itemId As Integer) As Item
            Using conn = Database.GetConnection()
                conn.Open()
                Using cmd As New MySqlCommand("SELECT ItemId,Name,Category,Unit,ReorderThreshold,UnitCost FROM Items WHERE ItemId=@id", conn)
                    cmd.Parameters.AddWithValue("@id", itemId)
                    Using r = cmd.ExecuteReader()
                        If r.Read() Then Return Map(r)
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Function Insert(item As Item) As Integer
            Using conn = Database.GetConnection()
                conn.Open()
                Using cmd As New MySqlCommand("INSERT INTO Items(Name,Category,Unit,ReorderThreshold,UnitCost) VALUES(@n,@c,@u,@r,@co)", conn)
                    AddParams(cmd, item)
                    cmd.ExecuteNonQuery()
                    Return Convert.ToInt32(cmd.LastInsertedId)
                End Using
            End Using
        End Function

        Public Sub Update(item As Item)
            Using conn = Database.GetConnection()
                conn.Open()
                Using cmd As New MySqlCommand("UPDATE Items SET Name=@n,Category=@c,Unit=@u,ReorderThreshold=@r,UnitCost=@co WHERE ItemId=@id", conn)
                    AddParams(cmd, item)
                    cmd.Parameters.AddWithValue("@id", item.ItemId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        ''' <summary>Deletes an item. Returns False (and does nothing) if it has transaction history.</summary>
        Public Function Delete(itemId As Integer) As Boolean
            Using conn = Database.GetConnection()
                conn.Open()
                Using check As New MySqlCommand("SELECT COUNT(*) FROM StockTransactions WHERE ItemId=@id", conn)
                    check.Parameters.AddWithValue("@id", itemId)
                    If Convert.ToInt32(check.ExecuteScalar()) > 0 Then Return False
                End Using
                Using cmd As New MySqlCommand("DELETE FROM Items WHERE ItemId=@id", conn)
                    cmd.Parameters.AddWithValue("@id", itemId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
            Return True
        End Function

        Private Sub AddParams(cmd As MySqlCommand, item As Item)
            cmd.Parameters.AddWithValue("@n", item.Name)
            cmd.Parameters.AddWithValue("@c", If(String.IsNullOrWhiteSpace(item.Category), CObj(DBNull.Value), item.Category))
            cmd.Parameters.AddWithValue("@u", If(String.IsNullOrWhiteSpace(item.Unit), CObj(DBNull.Value), item.Unit))
            cmd.Parameters.AddWithValue("@r", item.ReorderThreshold)
            cmd.Parameters.AddWithValue("@co", item.UnitCost)
        End Sub

        Private Function Map(r As MySqlDataReader) As Item
            Return New Item With {
                .ItemId = Convert.ToInt32(r("ItemId")),
                .Name = Convert.ToString(r("Name")),
                .Category = If(IsDBNull(r("Category")), "", Convert.ToString(r("Category"))),
                .Unit = If(IsDBNull(r("Unit")), "", Convert.ToString(r("Unit"))),
                .ReorderThreshold = Convert.ToInt32(r("ReorderThreshold")),
                .UnitCost = Convert.ToDecimal(r("UnitCost"))
            }
        End Function
    End Module
End Namespace