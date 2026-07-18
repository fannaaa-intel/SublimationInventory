Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace UI
    ''' <summary>Flat, custom-drawn sidebar navigation button with an Active state.</summary>
    Public Class NavButton
        Inherits Button

        Private _active As Boolean
        Private _hover As Boolean

        Public Property Active As Boolean
            Get
                Return _active
            End Get
            Set(value As Boolean)
                _active = value
                Invalidate()
            End Set
        End Property

        Public Sub New()
            SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                     ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
            FlatStyle = FlatStyle.Flat
            FlatAppearance.BorderSize = 0
            BackColor = Theme.SidebarBg
            ForeColor = Theme.TextLight
            Font = Theme.AppFont(10.5F)
            Cursor = Cursors.Hand
            Height = 46
        End Sub

        Protected Overrides Sub OnMouseEnter(e As EventArgs)
            MyBase.OnMouseEnter(e)
            _hover = True : Invalidate()
        End Sub

        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            MyBase.OnMouseLeave(e)
            _hover = False : Invalidate()
        End Sub

        Protected Overrides Sub OnPaint(pe As PaintEventArgs)
            Dim g = pe.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(Theme.SidebarBg)

            Dim rect = New Rectangle(12, 3, Width - 24, Height - 6)
            Dim back As Color
            If _active Then
                back = Theme.Accent
            ElseIf _hover Then
                back = Theme.SidebarBgDarker
            Else
                back = Theme.SidebarBg
            End If

            Using path = Theme.RoundedRect(rect, 8)
                Using b As New SolidBrush(back)
                    g.FillPath(b, path)
                End Using
            End Using

            ' Dark-blue accent stripe on the active screen's button
            If _active Then
                Using bar As New SolidBrush(Theme.Highlight)
                    g.FillRectangle(bar, rect.X, rect.Y + 6, 4, rect.Height - 12)
                End Using
            End If

            Dim txtColor = If(_active, Color.White, Theme.TextLight)
            Dim txtRect = New Rectangle(rect.X + 16, rect.Y, rect.Width - 20, rect.Height)
            TextRenderer.DrawText(g, Text, Font, txtRect, txtColor,
                                  TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)
        End Sub
    End Class
End Namespace
