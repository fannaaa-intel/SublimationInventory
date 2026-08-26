Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace UI
    ''' <summary>
    ''' Sidebar navigation button drawn as a cream "pill" on the navy rail, with a
    ''' line-art glyph on the left and a chevron on the right. Active state fills gold.
    ''' </summary>
    Public Class NavButton
        Inherits Button

        Private _active As Boolean
        Private _hover As Boolean
        Private _glyph As String = ""

        Public Property Active As Boolean
            Get
                Return _active
            End Get
            Set(value As Boolean)
                _active = value
                Invalidate()
            End Set
        End Property

        ''' <summary>Which line-art icon to draw: Dashboard, Stock, Supplies, Reports, Exit.</summary>
        Public Property Glyph As String
            Get
                Return _glyph
            End Get
            Set(value As String)
                _glyph = value
                Invalidate()
            End Set
        End Property

        ''' <summary>Styles the button as a destructive action (Sign Out).</summary>
        Public Property Danger As Boolean
            Get
                Return _danger
            End Get
            Set(value As Boolean)
                _danger = value
                Invalidate()
            End Set
        End Property
        Private _danger As Boolean

        Public Sub New()
            SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                     ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
            FlatStyle = FlatStyle.Flat
            FlatAppearance.BorderSize = 0
            BackColor = Theme.SidebarBg
            ForeColor = Theme.SidebarItemText
            Font = Theme.AppFont(10.5F, FontStyle.Bold)
            Cursor = Cursors.Hand
            Height = 52
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

            Dim rect = New Rectangle(14, 4, Width - 28, Height - 8)
            If rect.Width <= 0 OrElse rect.Height <= 0 Then Return

            If _danger Then
                PaintDanger(g, rect)
                Return
            End If

            ' Pill face: gold when active, cream at rest, slightly darker on hover.
            Dim face As Color
            Dim fore As Color
            If _active Then
                face = Theme.Accent
                fore = Color.White
            ElseIf _hover Then
                face = Theme.Shift(Theme.SidebarItemBg, -0.06F)
                fore = Theme.SidebarItemText
            Else
                face = Theme.SidebarItemBg
                fore = Theme.SidebarItemText
            End If

            ' Soft drop shadow so the pill lifts off the navy rail.
            Using shadow = Theme.RoundedRect(New Rectangle(rect.X + 1, rect.Y + 2, rect.Width, rect.Height), 10)
                Using b As New SolidBrush(Color.FromArgb(40, 0, 0, 0))
                    g.FillPath(b, shadow)
                End Using
            End Using

            Using path = Theme.RoundedRect(rect, 10)
                Using b As New SolidBrush(face)
                    g.FillPath(b, path)
                End Using
            End Using

            DrawGlyph(g, _glyph, rect.X + 14, rect.Y + (rect.Height \ 2) - 9, fore)

            Dim txtRect = New Rectangle(rect.X + 44, rect.Y, rect.Width - 48, rect.Height)
            TextRenderer.DrawText(g, Text, Font, txtRect, fore,
                                  TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)
        End Sub

        ''' <summary>Destructive style: solid red pill, brighter on hover.</summary>
        Private Sub PaintDanger(g As Graphics, rect As Rectangle)
            Dim fill = If(_hover, Theme.Shift(Theme.DangerRed, 0.12F), Theme.DangerRed)
            Dim fore = Color.White

            Using path = Theme.RoundedRect(rect, 10)
                Using b As New SolidBrush(fill)
                    g.FillPath(b, path)
                End Using
            End Using

            DrawGlyph(g, "Exit", rect.X + 14, rect.Y + (rect.Height \ 2) - 9, fore)

            Dim txtRect = New Rectangle(rect.X + 44, rect.Y, rect.Width - 48, rect.Height)
            TextRenderer.DrawText(g, Text, Font, txtRect, fore,
                                  TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)
        End Sub

        ''' <summary>18x18 line-art icons drawn with GDI+ so no icon font or image asset is needed.</summary>
        Private Sub DrawGlyph(g As Graphics, kind As String, x As Integer, y As Integer, c As Color)
            Using p As New Pen(c, 1.7F)
                p.StartCap = LineCap.Round
                p.EndCap = LineCap.Round
                p.LineJoin = LineJoin.Round

                Select Case kind
                    Case "Dashboard"
                        ' four panes
                        g.DrawRectangle(p, x, y, 7, 7)
                        g.DrawRectangle(p, x + 11, y, 7, 7)
                        g.DrawRectangle(p, x, y + 11, 7, 7)
                        g.DrawRectangle(p, x + 11, y + 11, 7, 7)

                    Case "Stock"
                        ' in / out arrows
                        g.DrawLine(p, x + 1, y + 5, x + 15, y + 5)
                        g.DrawLine(p, x + 11, y + 1, x + 15, y + 5)
                        g.DrawLine(p, x + 11, y + 9, x + 15, y + 5)
                        g.DrawLine(p, x + 17, y + 13, x + 3, y + 13)
                        g.DrawLine(p, x + 7, y + 9, x + 3, y + 13)
                        g.DrawLine(p, x + 7, y + 17, x + 3, y + 13)

                    Case "Supplies"
                        ' carton
                        g.DrawLine(p, x + 1, y + 5, x + 9, y + 1)
                        g.DrawLine(p, x + 9, y + 1, x + 17, y + 5)
                        g.DrawLine(p, x + 17, y + 5, x + 17, y + 14)
                        g.DrawLine(p, x + 17, y + 14, x + 9, y + 18)
                        g.DrawLine(p, x + 9, y + 18, x + 1, y + 14)
                        g.DrawLine(p, x + 1, y + 14, x + 1, y + 5)
                        g.DrawLine(p, x + 1, y + 5, x + 9, y + 9)
                        g.DrawLine(p, x + 17, y + 5, x + 9, y + 9)
                        g.DrawLine(p, x + 9, y + 9, x + 9, y + 18)

                    Case "Reports"
                        ' bar chart
                        g.DrawLine(p, x + 1, y + 17, x + 17, y + 17)
                        g.DrawLine(p, x + 4, y + 17, x + 4, y + 10)
                        g.DrawLine(p, x + 9, y + 17, x + 9, y + 4)
                        g.DrawLine(p, x + 14, y + 17, x + 14, y + 8)

                    Case "Exit"
                        g.DrawLine(p, x + 1, y + 1, x + 8, y + 1)
                        g.DrawLine(p, x + 1, y + 1, x + 1, y + 17)
                        g.DrawLine(p, x + 1, y + 17, x + 8, y + 17)
                        g.DrawLine(p, x + 6, y + 9, x + 17, y + 9)
                        g.DrawLine(p, x + 13, y + 5, x + 17, y + 9)
                        g.DrawLine(p, x + 13, y + 13, x + 17, y + 9)
                End Select
            End Using
        End Sub
    End Class
End Namespace
