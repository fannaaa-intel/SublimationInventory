Imports MySql.Data.MySqlClient
Imports System.Text
Imports SublimationInventory.Models
Imports System.Data.SqlClient

Namespace Data
    Public Module TransactionRepository

        Public Sub Insert(tx As StockTransaction)
            Using conn = Database.GetConnection()
                conn.Open()
                Using cmd As New MySqlCommand("INSERT INTO StockTransactions(ItemId,`Type`,Quantity,TransactionDate,Source,Reason,Notes) VALUES(@i,@t,@q,@d,@s,@r,@n)", conn)
                    cmd.Parameters.AddWithValue("@i", tx.ItemId)
                    cmd.Parameters.AddWithValue("@t", tx.Type)
                    cmd.Parameters.AddWithValue("@q", tx.Quantity)
                    cmd.Parameters.AddWithValue("@d", tx.TransactionDate)
                    cmd.Parameters.AddWithValue("@s", If(String.IsNullOrWhiteSpace(tx.Source), CObj(DBNull.Value), tx.Source))
                    cmd.Parameters.AddWithValue("@r", If(String.IsNullOrWhiteSpace(tx.Reason), CObj(DBNull.Value), tx.Reason))
                    cmd.Parameters.AddWithValue("@n", If(String.IsNullOrWhiteSpace(tx.Notes), CObj(DBNull.Value), tx.Notes))
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        ''' <summary>Transaction history with optional filters, newest first.</summary>
        Public Function Query(Optional itemId As Integer? = Nothing,
                              Optional type As String = Nothing,
                              Optional fromDate As DateTime? = Nothing,
                              Optional toDate As DateTime? = Nothing,
                              Optional topN As Integer? = Nothing) As List(Of StockTransaction)

            Dim list As New List(Of StockTransaction)()
            Dim sql As New StringBuilder()
            sql.Append("SELECT t.TransactionId,t.ItemId,t.`Type`,t.Quantity,t.TransactionDate,t.Source,t.Reason,t.Notes,i.Name AS ItemName ")
            sql.Append("FROM StockTransactions t INNER JOIN Items i ON i.ItemId=t.ItemId WHERE 1=1 ")
            If itemId.HasValue Then sql.Append("AND t.ItemId=@item ")
            If Not String.IsNullOrEmpty(type) Then sql.Append("AND t.`Type`=@type ")
            If fromDate.HasValue Then sql.Append("AND t.TransactionDate>=@from ")
            If toDate.HasValue Then sql.Append("AND t.TransactionDate<@to ")
            sql.Append("ORDER BY t.TransactionDate DESC, t.TransactionId DESC")
            If topN.HasValue Then sql.Append(" LIMIT " & topN.Value)

            Using conn = Database.GetConnection()
                conn.Open()
                Using cmd As New MySqlCommand(sql.ToString(), conn)
                    If itemId.HasValue Then cmd.Parameters.AddWithValue("@item", itemId.Value)
                    If Not String.IsNullOrEmpty(type) Then cmd.Parameters.AddWithValue("@type", type)
                    If fromDate.HasValue Then cmd.Parameters.AddWithValue("@from", fromDate.Value.Date)
                    If toDate.HasValue Then cmd.Parameters.AddWithValue("@to", toDate.Value.Date.AddDays(1))
                    Using r = cmd.ExecuteReader()
                        While r.Read()
                            list.Add(New StockTransaction With {
                                .TransactionId = Convert.ToInt32(r("TransactionId")),
                                .ItemId = Convert.ToInt32(r("ItemId")),
                                .Type = Convert.ToString(r("Type")),
                                .Quantity = Convert.ToInt32(r("Quantity")),
                                .TransactionDate = Convert.ToDateTime(r("TransactionDate")),
                                .Source = If(IsDBNull(r("Source")), "", Convert.ToString(r("Source"))),
                                .Reason = If(IsDBNull(r("Reason")), "", Convert.ToString(r("Reason"))),
                                .Notes = If(IsDBNull(r("Notes")), "", Convert.ToString(r("Notes"))),
                                .ItemName = Convert.ToString(r("ItemName"))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Function CountToday() As Integer
            Using conn = Database.GetConnection()
                conn.Open()
                Using cmd As New MySqlCommand("SELECT COUNT(*) FROM StockTransactions WHERE DATE(TransactionDate)=CURDATE()", conn)
                    Return Convert.ToInt32(cmd.ExecuteScalar())
                End Using
            End Using
        End Function

        Public Function CountThisMonth() As Integer
            Using conn = Database.GetConnection()
                conn.Open()
                Using cmd As New MySqlCommand("SELECT COUNT(*) FROM StockTransactions WHERE YEAR(TransactionDate)=YEAR(CURDATE()) AND MONTH(TransactionDate)=MONTH(CURDATE())", conn)
                    Return Convert.ToInt32(cmd.ExecuteScalar())
                End Using
            End Using
        End Function

        ''' <summary>Gross units moved per day for the last n days (dashboard chart).</summary>
        Public Function DailyMovement(days As Integer) As List(Of KeyValuePair(Of DateTime, Integer))
            Dim result As New List(Of KeyValuePair(Of DateTime, Integer))()
            Dim map As New Dictionary(Of DateTime, Integer)()
            Using conn = Database.GetConnection()
                conn.Open()
                Dim sql = "SELECT DATE(TransactionDate) AS D, SUM(Quantity) AS Q " &
                          "FROM StockTransactions WHERE TransactionDate>=@from " &
                          "GROUP BY DATE(TransactionDate)"
                Using cmd As New MySqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@from", DateTime.Now.Date.AddDays(-(days - 1)))
                    Using r = cmd.ExecuteReader()
                        While r.Read()
                            map(Convert.ToDateTime(r("D")).Date) = Convert.ToInt32(r("Q"))
                        End While
                    End Using
                End Using
            End Using
            For i = days - 1 To 0 Step -1
                Dim d = DateTime.Now.Date.AddDays(-i)
                result.Add(New KeyValuePair(Of DateTime, Integer)(d, If(map.ContainsKey(d), map(d), 0)))
            Next
            Return result
        End Function
    End Module
End Namespace