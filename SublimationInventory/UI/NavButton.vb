Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace UI
    ''' <summary>Flat, custom-drawn sidebar navigation button with Active and Danger states.</summary>
    Public Class NavButton
        Inherits Button

        Private _active As Boolean
        Private _hover As Boolean

        Private Shared ReadOnly DangerRed As Color = Color.FromArgb(206, 62, 52)
        Private Shared ReadOnly DangerSoft As Color = Color.FromArgb(226, 130, 122)

        Public Property Active As Boolean
            Get
                Return _active
            End Get
            Set(value As Boolean)
                _active = value
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

            Dim rect As Rectangle
            If _danger Then
                rect = New Rectangle(12, 3, Width - 24, Height - 20)   ' extra gap below
            Else
                rect = New Rectangle(12, 3, Width - 24, Height - 6)
            End If

            If _danger Then
                PaintDanger(g, rect)
                Return
            End If

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

        ''' <summary>Destructive style: outlined at rest, solid red on hover.</summary>
        Private Sub PaintDanger(g As Graphics, rect As Rectangle)
            Dim fill = If(_hover, Color.FromArgb(226, 76, 65), DangerRed)
            Dim fore = Color.White

            Using path = Theme.RoundedRect(rect, 8)
                Using b As New SolidBrush(fill)
                    g.FillPath(b, path)
                End Using
            End Using

            DrawExitIcon(g, rect.X + 16, rect.Y + (rect.Height \ 2) - 7, fore)

            Dim txtRect = New Rectangle(rect.X + 42, rect.Y, rect.Width - 46, rect.Height)
            TextRenderer.DrawText(g, Text, Font, txtRect, fore,
                                  TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)
        End Sub

        ''' <summary>Small "leaving through a door" glyph, drawn with lines so no font is required.</summary>
        Private Sub DrawExitIcon(g As Graphics, x As Integer, y As Integer, c As Color)
            Using p As New Pen(c, 1.6F)
                p.StartCap = LineCap.Round
                p.EndCap = LineCap.Round
                ' door frame (open on the right)
                g.DrawLine(p, x, y, x + 6, y)
                g.DrawLine(p, x, y, x, y + 14)
                g.DrawLine(p, x, y + 14, x + 6, y + 14)
                ' arrow pointing out
                g.DrawLine(p, x + 4, y + 7, x + 14, y + 7)
                g.DrawLine(p, x + 10, y + 3, x + 14, y + 7)
                g.DrawLine(p, x + 10, y + 11, x + 14, y + 7)
            End Using
        End Sub
    End Class
End Namespace