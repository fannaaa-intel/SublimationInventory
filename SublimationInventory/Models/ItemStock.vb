Namespace Models
    ''' <summary>
    ''' View model pairing an Item with its computed on-hand quantity.
    ''' On-hand is never stored - it always comes from InventoryService.
    ''' </summary>
    Public Class ItemStock
        Public Property Item As Item
        Public Property OnHand As Integer

        ''' <summary>
        ''' True when the item has a reorder level set and has fallen to or below it.
        ''' An item with no threshold (0) is never "low" - otherwise every item that is
        ''' simply out of stock, including one just created, would raise a false alert.
        ''' </summary>
        Public ReadOnly Property IsLowStock As Boolean
            Get
                Return Item.ReorderThreshold > 0 AndAlso OnHand <= Item.ReorderThreshold
            End Get
        End Property

        Public ReadOnly Property StockValue As Decimal
            Get
                Return OnHand * Item.UnitCost
            End Get
        End Property
    End Class
End Namespace
