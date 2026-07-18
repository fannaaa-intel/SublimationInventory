Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports SublimationInventory.Data
Imports SublimationInventory.Models
Imports SublimationInventory.UI

Namespace Forms
    ''' <summary>Shell window: persistent sidebar + swappable content panel.</summary>
    Public Class MainForm
        Inherits Form

        Private ReadOnly _user As User
        Private contentPanel As Panel
        Private ReadOnly navButtons As New List(Of NavButton)()
        Private _avatar As Image
        Private _sidebar As Panel
        Private ReadOnly _avatarRect As New Rectangle((230 - 60) \ 2, 32, 60, 60)

        Public Sub New(user As User)
            _user = user
            InitializeComponent()
            ShowScreen("Dashboard")
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "Sublimation Inventory"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.ClientSize = New Size(1140, 720)
            Me.MinimumSize = New Size(1160, 700)
            Me.BackColor = Theme.ContentBg
            Me.Font = Theme.AppFont(10.0F)

            _sidebar = New Panel With {.Dock = DockStyle.Left, .Width = 230, .BackColor = Theme.SidebarBg,
                                       .Padding = New Padding(0, 0, 0, 14)}
            AddHandler _sidebar.Paint, AddressOf PaintSidebarProfile
            AddHandler _sidebar.MouseClick, AddressOf OnSidebarClick
            AddHandler _sidebar.MouseMove, Sub(s, ev) _sidebar.Cursor = If(_avatarRect.Contains(ev.Location), Cursors.Hand, Cursors.Default)
            Me.Controls.Add(_sidebar)
            LoadSavedAvatar()

            contentPanel = New Panel With {.Dock = DockStyle.Fill, .BackColor = Theme.ContentBg}
            GetType(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance Or System.Reflection.BindingFlags.NonPublic).SetValue(contentPanel, True, Nothing)
            Me.Controls.Add(contentPanel)
            contentPanel.BringToFront()

            ' Nav order + labels must match the spec exactly.
            Dim labels = New String() {"Dashboard", "Stock In/Out", "Supplies", "Reports"}
            Dim y = 150
            For Each lbl In labels
                Dim nb As New NavButton With {.Text = lbl, .Left = 0, .Top = y, .Width = _sidebar.Width, .Tag = lbl}
                AddHandler nb.Click, Sub(s, ev) ShowScreen(CStr(DirectCast(s, NavButton).Tag))
                _sidebar.Controls.Add(nb)
                navButtons.Add(nb)
                y += 52
            Next

            Dim btnLogout As New NavButton With {.Text = "Sign Out", .Dock = DockStyle.Bottom, .Tag = "Logout",
                                                 .Danger = True, .Height = 60}
            AddHandler btnLogout.Click, Sub(s, ev) DoLogout()
            _sidebar.Controls.Add(btnLogout)
        End Sub

        Private Sub PaintSidebarProfile(sender As Object, e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim d = 60
            Dim cx = (230 - d) \ 2
            Dim cy = 32

            If _avatar IsNot Nothing Then
                ' Clip to a circle and draw the uploaded photo inside it.
                Using clip As New GraphicsPath()
                    clip.AddEllipse(cx, cy, d, d)
                    Dim oldClip = g.Clip
                    g.SetClip(clip)
                    g.DrawImage(_avatar, cx, cy, d, d)
                    g.Clip = oldClip
                End Using
                Using pen As New Pen(Color.FromArgb(80, Color.White), 1.5F)
                    g.DrawEllipse(pen, cx, cy, d, d)
                End Using
            Else
                ' Default: initials on the accent circle.
                Using b As New SolidBrush(Theme.Accent)
                    g.FillEllipse(b, cx, cy, d, d)
                End Using
                Using f = Theme.AppFont(18.0F, FontStyle.Bold)
                    TextRenderer.DrawText(g, Initials(), f, New Rectangle(cx, cy, d, d), Color.White,
                                          TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
                End Using
            End If

            Using f = Theme.AppFont(11.0F, FontStyle.Bold)
                TextRenderer.DrawText(g, "Shop Inventory", f, New Rectangle(0, cy + d + 10, 230, 20), Theme.TextLight, TextFormatFlags.HorizontalCenter)
            End Using
        End Sub

        ''' <summary>Reads this user's avatar from the database.</summary>
        Private Sub LoadSavedAvatar()
            Try
                Dim data = UserRepository.GetProfileImage(_user.UserId)
                If data IsNot Nothing AndAlso data.Length > 0 Then
                    Using ms As New IO.MemoryStream(data)
                        _avatar = New Bitmap(Image.FromStream(ms))
                    End Using
                End If
            Catch
                _avatar = Nothing
            End Try
        End Sub

        Private Sub OnSidebarClick(sender As Object, e As MouseEventArgs)
            If Not _avatarRect.Contains(e.Location) Then Return
            Using ofd As New OpenFileDialog()
                ofd.Title = "Choose a profile picture"
                ofd.Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
                If ofd.ShowDialog(Me) <> DialogResult.OK Then Return
                Try
                    Dim resized As Bitmap
                    Using tmp = Image.FromFile(ofd.FileName)
                        resized = ResizeSquare(tmp, 256)
                    End Using

                    Dim bytes As Byte()
                    Using ms As New IO.MemoryStream()
                        resized.Save(ms, Imaging.ImageFormat.Png)
                        bytes = ms.ToArray()
                    End Using

                    UserRepository.SaveProfileImage(_user.UserId, bytes)

                    If _avatar IsNot Nothing Then _avatar.Dispose()
                    _avatar = resized
                    _sidebar.Invalidate()
                Catch ex As Exception
                    AppModal.ErrorBox(Me, "That image couldn't be saved. Try a different file." & vbCrLf & vbCrLf & ex.Message, "Couldn't set picture")
                End Try
            End Using
        End Sub

        ''' <summary>Centre-crops to a square and scales down, so stored images stay small.</summary>
        Private Function ResizeSquare(src As Image, size As Integer) As Bitmap
            Dim side = Math.Min(src.Width, src.Height)
            Dim sx = (src.Width - side) \ 2
            Dim sy = (src.Height - side) \ 2
            Dim bmp As New Bitmap(size, size)
            Using g = Graphics.FromImage(bmp)
                g.InterpolationMode = InterpolationMode.HighQualityBicubic
                g.PixelOffsetMode = PixelOffsetMode.HighQuality
                g.SmoothingMode = SmoothingMode.HighQuality
                g.DrawImage(src, New Rectangle(0, 0, size, size), New Rectangle(sx, sy, side, side), GraphicsUnit.Pixel)
            End Using
            Return bmp
        End Function

        Private Function Initials() As String
            Dim src = If(String.IsNullOrWhiteSpace(_user.FullName), _user.Username, _user.FullName)
            Dim parts = src.Trim().Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
            If parts.Length = 0 Then Return "?"
            If parts.Length = 1 Then Return parts(0).Substring(0, 1).ToUpper()
            Return (parts(0).Substring(0, 1) & parts(parts.Length - 1).Substring(0, 1)).ToUpper()
        End Function

        Private Sub ShowScreen(name As String)
            contentPanel.SuspendLayout()
            contentPanel.Controls.Clear()
            Dim ctrl As Control = Nothing
            Select Case name
                Case "Dashboard" : ctrl = New DashboardControl()
                Case "Stock In/Out" : ctrl = New StockInOutControl()
                Case "Supplies" : ctrl = New SuppliesControl()
                Case "Reports" : ctrl = New ReportsControl()
            End Select
            If ctrl IsNot Nothing Then
                ctrl.Dock = DockStyle.Fill
                contentPanel.Controls.Add(ctrl)
            End If
            contentPanel.ResumeLayout()

            For Each nb In navButtons
                nb.Active = (CStr(nb.Tag) = name)
            Next
        End Sub

        Private Sub DoLogout()
            If Not AppModal.Confirm(Me, "Sign out?", "You'll be returned to the login screen.", "Sign Out") Then Return
            Me.Hide()
            Using login As New LoginForm()
                If login.ShowDialog() = DialogResult.OK Then
                    Me.Show()
                    ShowScreen("Dashboard")
                Else
                    Application.Exit()
                End If
            End Using
        End Sub
    End Class
End Namespace