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
        Private _sidebar As Panel

        ''' <summary>Sidebar geometry - every layout calculation derives from these.</summary>
        Private Const SidebarWidth As Integer = 236
        Private Const LogoSize As Integer = 78
        Private Const LogoTop As Integer = 30

        Public Sub New(user As User)
            _user = user
            InitializeComponent()
            ShowScreen("Dashboard")
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "RJA Sportswear - Inventory Management"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.ClientSize = New Size(1280, 800)
            Me.MinimumSize = New Size(1100, 680)
            Me.BackColor = Theme.ContentBg
            Me.Font = Theme.AppFont(10.0F)
            Dim ico = Branding.CreateWindowIcon()
            If ico IsNot Nothing Then Me.Icon = ico
            ' Open filling the whole screen; the user can still restore/resize.
            Me.WindowState = FormWindowState.Maximized

            _sidebar = New Panel With {.Dock = DockStyle.Left, .Width = SidebarWidth, .BackColor = Theme.SidebarBg,
                                       .Padding = New Padding(0, 0, 0, 14)}
            AddHandler _sidebar.Paint, AddressOf PaintSidebarProfile
            Me.Controls.Add(_sidebar)

            contentPanel = New Panel With {.Dock = DockStyle.Fill, .BackColor = Theme.ContentBg}
            GetType(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance Or System.Reflection.BindingFlags.NonPublic).SetValue(contentPanel, True, Nothing)
            Me.Controls.Add(contentPanel)
            contentPanel.BringToFront()

            ' Nav order + labels must match the spec exactly.
            Dim labels = New String() {"Dashboard", "Stock In/Out", "Supplies", "Reports"}
            Dim glyphs = New String() {"Dashboard", "Stock", "Supplies", "Reports"}
            Dim y = LogoTop + LogoSize + 66
            For i = 0 To labels.Length - 1
                Dim nb As New NavButton With {.Text = labels(i), .Glyph = glyphs(i), .Left = 0, .Top = y,
                                              .Width = _sidebar.Width, .Tag = labels(i)}
                AddHandler nb.Click, Sub(s, ev) ShowScreen(CStr(DirectCast(s, NavButton).Tag))
                _sidebar.Controls.Add(nb)
                navButtons.Add(nb)
                y += 58
            Next

            Dim btnLogout As New NavButton With {.Text = "Sign Out", .Dock = DockStyle.Bottom, .Tag = "Logout",
                                                 .Danger = True, .Height = 62}
            AddHandler btnLogout.Click, Sub(s, ev) DoLogout()
            _sidebar.Controls.Add(btnLogout)
        End Sub

        Private Sub PaintSidebarProfile(sender As Object, e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim d = LogoSize
            Dim cx = (SidebarWidth - d) \ 2
            Dim cy = LogoTop

            ' Company logo in a gold ring - branding, not a user avatar.
            Branding.DrawCircular(g, cx, cy, d, Theme.Accent, 2.5F, "RJA")

            Using f = Theme.AppFont(13.0F, FontStyle.Bold)
                TextRenderer.DrawText(g, "RJA SPORTSWEAR", f, New Rectangle(8, cy + d + 14, SidebarWidth - 16, 22),
                                      Theme.TextLight, TextFormatFlags.HorizontalCenter Or TextFormatFlags.EndEllipsis Or
                                      TextFormatFlags.NoPrefix)
            End Using

            ' Who is signed in, kept small underneath the brand.
            Dim nameText = If(String.IsNullOrWhiteSpace(_user.FullName), _user.Username, _user.FullName)
            Using f = Theme.AppFont(8.5F)
                TextRenderer.DrawText(g, "Signed in as " & nameText, f,
                                      New Rectangle(8, cy + d + 36, SidebarWidth - 16, 18),
                                      Theme.TextOnNavy, TextFormatFlags.HorizontalCenter Or TextFormatFlags.EndEllipsis Or
                                      TextFormatFlags.NoPrefix)
            End Using
        End Sub

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