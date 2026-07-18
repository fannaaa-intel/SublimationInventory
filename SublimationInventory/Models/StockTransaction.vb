Namespace Models
    ''' <summary>A single stock movement. Type is "In" or "Out".</summary>
    Public Class StockTransaction
        Public Property TransactionId As Integer
        Public Property ItemId As Integer
        Public Property Type As String
        Public Property Quantity As Integer
        Public Property TransactionDate As DateTime
        Public Property Source As String   ' In only (nullable)
        Public Property Reason As String   ' Out only (nullable)
        Public Property Notes As String
        Public Property ItemName As String ' join convenience for display
    End Class
End Namespace
