Namespace Models
    ''' <summary>
    ''' View model pairing an Item with its computed on-hand quantity.
    ''' On-hand is never stored - it always comes from InventoryService.
    ''' </summary>
    Public Class ItemStock
        Public Property Item As Item
        Public Property OnHand As Integer

        Public ReadOnly Property IsLowStock As Boolean
            Get
                Return OnHand <= Item.ReorderThreshold
            End Get
        End Property

        Public ReadOnly Property StockValue As Decimal
            Get
                Return OnHand * Item.UnitCost
            End Get
        End Property
    End Class
End Namespace
