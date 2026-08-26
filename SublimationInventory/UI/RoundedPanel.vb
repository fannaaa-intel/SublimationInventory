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

        Public Property CornerRadius As Integer = 12
        Public Property FillColor As Color = Theme.CardBg
        Public Property BorderColor As Color = Theme.BorderColor
        Public Property DrawBorder As Boolean = True

        ''' <summary>Draws a soft drop shadow so cards lift off the cream page.</summary>
        Public Property DrawShadow As Boolean = True

        ''' <summary>Optional gold accent strip along the top edge of the card.</summary>
        Public Property AccentStrip As Boolean = False

        Public Sub New()
            SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                     ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
            BackColor = Theme.ContentBg
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.Clear(BackColor)
            g.SmoothingMode = SmoothingMode.AntiAlias

            Dim inset = If(DrawShadow, 3, 0)
            Dim rect = New Rectangle(0, 0, Width - 1 - inset, Height - 1 - inset)
            If rect.Width <= 0 OrElse rect.Height <= 0 Then Return

            ' Layered translucent outlines approximate a blur without a bitmap pass.
            If DrawShadow Then
                For i = inset To 1 Step -1
                    Using sp = Theme.RoundedRect(New Rectangle(i, i, rect.Width, rect.Height), CornerRadius)
                        Using b As New SolidBrush(Color.FromArgb(10, 40, 35, 20))
                            g.FillPath(b, sp)
                        End Using
                    End Using
                Next
            End If

            Using path = Theme.RoundedRect(rect, CornerRadius)
                Using b As New SolidBrush(FillColor)
                    g.FillPath(b, path)
                End Using

                If AccentStrip Then
                    Dim old = g.Clip
                    g.SetClip(path)
                    Using b As New SolidBrush(Theme.Accent)
                        g.FillRectangle(b, rect.X, rect.Y, rect.Width, 4)
                    End Using
                    g.Clip = old
                End If

                If DrawBorder Then
                    Using p As New Pen(BorderColor)
                        g.DrawPath(p, path)
                    End Using
                End If
            End Using
        End Sub
    End Class
End Namespace
