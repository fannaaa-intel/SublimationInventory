Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace UI
    ''' <summary>
    ''' A card panel with rounded corners. BackColor should match the surrounding
    ''' background (painted into the corners); FillColor is the card face.
    ''' </summary>
    Public Class RoundedPanel
        Inherits Panel

        Public Property CornerRadius As Integer = 10
        Public Property FillColor As Color = Theme.CardBg
        Public Property BorderColor As Color = Theme.BorderColor
        Public Property DrawBorder As Boolean = True

        Public Sub New()
            SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                     ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
            BackColor = Theme.ContentBg
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.Clear(BackColor)
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim rect = New Rectangle(0, 0, Width - 1, Height - 1)
            Using path = Theme.RoundedRect(rect, CornerRadius)
                Using b As New SolidBrush(FillColor)
                    g.FillPath(b, path)
                End Using
                If DrawBorder Then
                    Using p As New Pen(BorderColor)
                        g.DrawPath(p, path)
                    End Using
                End If
            End Using
        End Sub
    End Class
End Namespace
