Imports System.Drawing
Imports System.Drawing.Drawing2D

Namespace UI
    ''' <summary>Central colour palette + drawing helpers (dark maroon / warm amber).</summary>
    Public Module Theme
        Public ReadOnly SidebarBg As Color = Color.FromArgb(58, 33, 30)        ' dark maroon/brown
        Public ReadOnly SidebarBgDarker As Color = Color.FromArgb(45, 25, 23)
        Public ReadOnly Accent As Color = Color.FromArgb(232, 134, 61)         ' warm orange/amber
        Public ReadOnly AccentHover As Color = Color.FromArgb(245, 158, 82)
        Public ReadOnly ContentBg As Color = Color.FromArgb(245, 243, 241)     ' light content area
        Public ReadOnly CardBg As Color = Color.White
        Public ReadOnly SecondaryDark As Color = Color.FromArgb(30, 20, 18)    ' near-black panels
        Public ReadOnly TextDark As Color = Color.FromArgb(45, 35, 32)
        Public ReadOnly TextLight As Color = Color.FromArgb(245, 240, 236)
        Public ReadOnly TextMuted As Color = Color.FromArgb(150, 130, 123)
        Public ReadOnly BorderColor As Color = Color.FromArgb(228, 222, 218)
        Public ReadOnly LowStockBg As Color = Color.FromArgb(255, 224, 204)    ' low-stock row highlight
        Public ReadOnly LowStockText As Color = Color.FromArgb(178, 74, 20)
        Public ReadOnly HoverRow As Color = Color.FromArgb(250, 243, 237)      ' warm hover tint
        Public ReadOnly SelectedRow As Color = Color.FromArgb(248, 232, 220)   ' active/clicked row
        Public ReadOnly Highlight As Color = Color.FromArgb(37, 99, 235)       ' dark blue accent
        Public ReadOnly BorderStrong As Color = Color.FromArgb(176, 168, 161)   ' visible grid lines / dialog borders

        Public Const FontName As String = "Segoe UI"

        Public Function AppFont(size As Single, Optional style As FontStyle = FontStyle.Regular) As Font
            Return New Font(FontName, size, style)
        End Function

        Public Function RoundedRect(bounds As Rectangle, radius As Integer) As GraphicsPath
            Dim path As New GraphicsPath()
            If radius <= 0 Then
                path.AddRectangle(bounds)
                path.CloseFigure()
                Return path
            End If
            Dim d = radius * 2
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90)
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90)
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90)
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90)
            path.CloseFigure()
            Return path
        End Function
    End Module
End Namespace