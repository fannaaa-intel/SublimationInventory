Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace UI
    Public Enum ModalKind
        Info
        Success
        Warning
        Problem
        Question
    End Enum

    ''' <summary>Themed replacement for MessageBox: rounded, draggable, with a clearly visible border.</summary>
    Public Class AppModal
        Inherits Form

        Private _drag As Boolean
        Private _dragStart As Point
        Private _formStart As Point

        Private Sub New(kind As ModalKind, heading As String, message As String,
                        confirmText As String, showCancel As Boolean, danger As Boolean)
            Me.FormBorderStyle = FormBorderStyle.None
            Me.ShowInTaskbar = False
            Me.BackColor = Color.White
            Me.Font = Theme.AppFont(10.0F)
            Me.ClientSize = New Size(412, 200)
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer, True)

            Dim accent = ColorFor(kind, danger)

            Me.Controls.Add(New Panel With {.Dock = DockStyle.Top, .Height = 6, .BackColor = accent})

            Dim icon As New Panel With {.Size = New Size(46, 46), .Location = New Point(28, 34), .BackColor = Color.White}
            AddHandler icon.Paint, Sub(s, e)
                                       e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                       Using b As New SolidBrush(Color.FromArgb(28, accent))
                                           e.Graphics.FillEllipse(b, 0, 0, 45, 45)
                                       End Using
                                       Using f = Theme.AppFont(20.0F, FontStyle.Bold)
                                           TextRenderer.DrawText(e.Graphics, GlyphFor(kind), f, New Rectangle(0, 0, 46, 46),
                                               accent, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
                                       End Using
                                   End Sub
            Me.Controls.Add(icon)

            Me.Controls.Add(New Label With {.Text = heading, .Font = Theme.AppFont(13.0F, FontStyle.Bold),
                                            .ForeColor = Theme.TextDark, .AutoSize = True, .Location = New Point(90, 34)})
            Dim lblMsg As New Label With {.Text = message, .Font = Theme.AppFont(9.75F), .ForeColor = Theme.TextMuted,
                                          .Location = New Point(90, 62), .AutoSize = True, .MaximumSize = New Size(296, 0)}
            Me.Controls.Add(lblMsg)

            Dim contentBottom = Math.Max(icon.Bottom, lblMsg.Bottom)
            Dim btnTop = contentBottom + 20
            Me.ClientSize = New Size(412, btnTop + 38 + 18)

            Dim btnConfirm As New Button With {.Text = confirmText, .Size = New Size(112, 38),
                                               .FlatStyle = FlatStyle.Flat, .BackColor = accent, .ForeColor = Color.White,
                                               .Font = Theme.AppFont(10.0F, FontStyle.Bold), .Cursor = Cursors.Hand, .DialogResult = DialogResult.Yes}
            btnConfirm.FlatAppearance.BorderSize = 0
            btnConfirm.Location = New Point(Me.ClientSize.Width - 24 - btnConfirm.Width, btnTop)
            Me.Controls.Add(btnConfirm)
            Me.AcceptButton = btnConfirm

            If showCancel Then
                Dim btnCancel As New Button With {.Text = "Cancel", .Size = New Size(94, 38),
                                                  .Location = New Point(btnConfirm.Left - 10 - 94, btnTop), .DialogResult = DialogResult.No}
                UiHelpers.StyleSecondaryButton(btnCancel)
                Me.Controls.Add(btnCancel)
                Me.CancelButton = btnCancel
            Else
                Me.CancelButton = btnConfirm
            End If

            AddHandler Me.MouseDown, AddressOf DragDown
            AddHandler Me.MouseMove, AddressOf DragMove
            AddHandler Me.MouseUp, Sub(s, e) _drag = False
        End Sub

        Private Shared Function ColorFor(kind As ModalKind, danger As Boolean) As Color
            If danger Then Return Color.FromArgb(200, 55, 45)
            Select Case kind
                Case ModalKind.Success : Return Color.FromArgb(34, 153, 84)
                Case ModalKind.Warning : Return Color.FromArgb(200, 120, 20)
                Case ModalKind.Problem : Return Color.FromArgb(200, 55, 45)
                Case Else : Return Theme.Highlight   ' Info / Question -> blue
            End Select
        End Function

        Private Shared Function GlyphFor(kind As ModalKind) As String
            Select Case kind
                Case ModalKind.Success : Return "✓"
                Case ModalKind.Warning : Return "!"
                Case ModalKind.Problem : Return "✕"
                Case ModalKind.Question : Return "?"
                Case Else : Return "i"
            End Select
        End Function

        Private Sub DragDown(s As Object, e As MouseEventArgs)
            If e.Button = MouseButtons.Left Then _drag = True : _dragStart = Cursor.Position : _formStart = Me.Location
        End Sub
        Private Sub DragMove(s As Object, e As MouseEventArgs)
            If _drag Then Me.Location = New Point(_formStart.X + (Cursor.Position.X - _dragStart.X),
                                                  _formStart.Y + (Cursor.Position.Y - _dragStart.Y))
        End Sub

        Protected Overrides Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Using path = Theme.RoundedRect(New Rectangle(0, 0, Width, Height), 14)
                Me.Region = New Region(path)
            End Using
        End Sub
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            Using p As New Pen(Theme.BorderStrong, 2.0F)   ' clearly visible border
                Using path = Theme.RoundedRect(New Rectangle(1, 1, Width - 3, Height - 3), 13)
                    e.Graphics.DrawPath(p, path)
                End Using
            End Using
        End Sub

        ' ---------- public API ----------
        Private Shared Function Run(owner As IWin32Window, kind As ModalKind, heading As String, message As String,
                                    confirmText As String, showCancel As Boolean, danger As Boolean) As DialogResult
            Using dlg As New AppModal(kind, heading, message, confirmText, showCancel, danger)
                dlg.StartPosition = If(owner IsNot Nothing, FormStartPosition.CenterParent, FormStartPosition.CenterScreen)
                Return If(owner IsNot Nothing, dlg.ShowDialog(owner), dlg.ShowDialog())
            End Using
        End Function

        Public Shared Sub Info(owner As IWin32Window, message As String, Optional heading As String = "Information")
            Run(owner, ModalKind.Info, heading, message, "OK", False, False)
        End Sub
        Public Shared Sub Success(owner As IWin32Window, message As String, Optional heading As String = "Done")
            Run(owner, ModalKind.Success, heading, message, "OK", False, False)
        End Sub
        Public Shared Sub Warn(owner As IWin32Window, message As String, Optional heading As String = "Please check")
            Run(owner, ModalKind.Warning, heading, message, "OK", False, False)
        End Sub
        Public Shared Sub ErrorBox(owner As IWin32Window, message As String, Optional heading As String = "Something went wrong")
            Run(owner, ModalKind.Problem, heading, message, "OK", False, False)
        End Sub
        Public Shared Function Confirm(owner As IWin32Window, heading As String, message As String,
                                       Optional confirmText As String = "Confirm", Optional danger As Boolean = False) As Boolean
            Return Run(owner, If(danger, ModalKind.Problem, ModalKind.Question), heading, message, confirmText, True, danger) = DialogResult.Yes
        End Function
    End Class
End Namespace