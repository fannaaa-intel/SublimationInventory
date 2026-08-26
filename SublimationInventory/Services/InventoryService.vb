Imports MySql.Data.MySqlClient
Imports SublimationInventory.Data
Imports SublimationInventory.Models

Namespace Services
    ''' <summary>
    ''' SINGLE source of truth for computed on-hand quantities.
    ''' On-hand = SUM(In.Quantity) - SUM(Out.Quantity) per item.
    ''' Dashboard, Supplies and Reports all derive stock levels from here -
    ''' there is no separate calculation path and no stored running total.
    ''' </summary>
    Public Module InventoryService

        Public Function GetOnHandByItem() As Dictionary(Of Integer, Integer)
            Dim map As New Dictionary(Of Integer, Integer)()
            Using conn = Database.GetConnection()
                conn.Open()
                Dim sql = "SELECT ItemId, SUM(CASE WHEN `Type`='In' THEN Quantity ELSE -Quantity END) AS OnHand " &
                          "FROM StockTransactions GROUP BY ItemId"
                Using cmd As New MySqlCommand(sql, conn)
                    Using r = cmd.ExecuteReader()
                        While r.Read()
                            map(Convert.ToInt32(r("ItemId"))) = Convert.ToInt32(r("OnHand"))
                        End While
                    End Using
                End Using
            End Using
            Return map
        End Function

        Public Function GetOnHand(itemId As Integer) As Integer
            Using conn = Database.GetConnection()
                conn.Open()
                Dim sql = "SELECT IFNULL(SUM(CASE WHEN `Type`='In' THEN Quantity ELSE -Quantity END),0) " &
                          "FROM StockTransactions WHERE ItemId=@id"
                Using cmd As New MySqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@id", itemId)
                    Return Convert.ToInt32(cmd.ExecuteScalar())
                End Using
            End Using
        End Function

        Public Function GetItemsWithStock() As List(Of ItemStock)
            Dim onHand = GetOnHandByItem()
            Dim list As New List(Of ItemStock)()
            For Each it In ItemRepository.GetAll()
                Dim qty = If(onHand.ContainsKey(it.ItemId), onHand(it.ItemId), 0)
                list.Add(New ItemStock With {.Item = it, .OnHand = qty})
            Next
            Return list
        End Function

        Public Function GetLowStockItems() As List(Of ItemStock)
            Return GetItemsWithStock().Where(Function(x) x.IsLowStock).ToList()
        End Function

        Public Function GetLowStockCount() As Integer
            Return GetLowStockItems().Count
        End Function

        ''' <summary>
        ''' True if at least one item has a reorder level configured. A zero low-stock
        ''' count means something very different when no thresholds exist at all, and the
        ''' dashboard says so rather than implying everything is healthy.
        ''' </summary>
        Public Function AnyReorderLevelSet() As Boolean
            Return ItemRepository.GetAll().Any(Function(i) i.ReorderThreshold > 0)
        End Function

        Public Function GetTotalUnitsInStock() As Integer
            Return GetItemsWithStock().Sum(Function(x) Math.Max(0, x.OnHand))
        End Function

        Public Function GetTotalInventoryValue() As Decimal
            Return GetItemsWithStock().Sum(Function(x) x.StockValue)
        End Function
    End Module
End Namespace