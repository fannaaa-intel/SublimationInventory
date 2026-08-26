Imports System.Drawing
Imports System.Drawing.Drawing2D

Namespace UI
    ''' <summary>Central colour palette + drawing helpers (deep navy / gold on cream).</summary>
    Public Module Theme
        ' ---------- Sidebar (deep navy) ----------
        Public ReadOnly SidebarBg As Color = Color.FromArgb(26, 27, 64)         ' deep navy
        Public ReadOnly SidebarBgDarker As Color = Color.FromArgb(19, 20, 50)   ' hover / pressed
        Public ReadOnly SidebarItemBg As Color = Color.FromArgb(238, 236, 228)  ' cream nav pill
        Public ReadOnly SidebarItemText As Color = Color.FromArgb(26, 27, 64)   ' navy text on cream pill

        ' ---------- Accent (gold) ----------
        Public ReadOnly Accent As Color = Color.FromArgb(198, 160, 68)          ' gold
        Public ReadOnly AccentHover As Color = Color.FromArgb(219, 182, 92)     ' lighter gold
        Public ReadOnly AccentDeep As Color = Color.FromArgb(163, 128, 44)      ' pressed gold

        ' ---------- Surfaces ----------
        Public ReadOnly ContentBg As Color = Color.FromArgb(245, 241, 230)      ' cream page
        Public ReadOnly CardBg As Color = Color.FromArgb(252, 250, 244)         ' warm card face
        Public ReadOnly CardBgAlt As Color = Color.FromArgb(238, 233, 219)      ' inset / tinted card
        Public ReadOnly SecondaryDark As Color = Color.FromArgb(26, 27, 64)     ' navy stat panel

        ' ---------- Text ----------
        Public ReadOnly TextDark As Color = Color.FromArgb(28, 29, 56)
        Public ReadOnly TextLight As Color = Color.FromArgb(247, 245, 238)
        Public ReadOnly TextMuted As Color = Color.FromArgb(128, 126, 140)
        Public ReadOnly TextOnNavy As Color = Color.FromArgb(198, 196, 214)     ' muted text on navy

        ' ---------- Lines ----------
        Public ReadOnly BorderColor As Color = Color.FromArgb(226, 220, 204)
        Public ReadOnly BorderStrong As Color = Color.FromArgb(196, 187, 165)

        ' ---------- Status ----------
        Public ReadOnly LowStockBg As Color = Color.FromArgb(250, 226, 205)     ' low-stock row
        Public ReadOnly LowStockText As Color = Color.FromArgb(158, 74, 20)
        Public ReadOnly DangerRed As Color = Color.FromArgb(199, 62, 58)        ' critical bar / sign out
        Public ReadOnly WarnAmber As Color = Color.FromArgb(214, 148, 46)       ' warning bar
        Public ReadOnly OkGreen As Color = Color.FromArgb(56, 142, 92)          ' healthy bar / stock in

        ' ---------- Rows ----------
        Public ReadOnly HoverRow As Color = Color.FromArgb(243, 238, 224)
        Public ReadOnly SelectedRow As Color = Color.FromArgb(232, 224, 202)
        Public ReadOnly Highlight As Color = Color.FromArgb(43, 45, 96)         ' navy selection

        ' ---------- Chart series ----------
        Public ReadOnly ChartLine As Color = Color.FromArgb(43, 45, 96)
        Public ReadOnly ChartFill As Color = Color.FromArgb(38, 198, 160, 68)
        Public ReadOnly ChartGrid As Color = Color.FromArgb(228, 222, 206)
        Public ReadOnly ChartBar As Color = Color.FromArgb(58, 60, 118)

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
            If d > bounds.Width Then d = bounds.Width
            If d > bounds.Height Then d = bounds.Height
            If d <= 0 Then
                path.AddRectangle(bounds)
                path.CloseFigure()
                Return path
            End If
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90)
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90)
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90)
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90)
            path.CloseFigure()
            Return path
        End Function

        ''' <summary>Shifts a colour toward black (amount &lt; 0) or white (amount &gt; 0), 0..1.</summary>
        Public Function Shift(c As Color, amount As Single) As Color
            If amount >= 0 Then
                Return Color.FromArgb(c.A,
                    CInt(c.R + (255 - c.R) * amount),
                    CInt(c.G + (255 - c.G) * amount),
                    CInt(c.B + (255 - c.B) * amount))
            End If
            Dim k = 1.0F + amount
            Return Color.FromArgb(c.A, CInt(c.R * k), CInt(c.G * k), CInt(c.B * k))
        End Function
    End Module
End Namespace
