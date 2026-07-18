Namespace Models
    ''' <summary>An inventory item / supply (blank, ink, transfer paper, etc.).</summary>
    Public Class Item
        Public Property ItemId As Integer
        Public Property Name As String
        Public Property Category As String
        Public Property Unit As String
        Public Property ReorderThreshold As Integer
        Public Property UnitCost As Decimal
    End Class
End Namespace
