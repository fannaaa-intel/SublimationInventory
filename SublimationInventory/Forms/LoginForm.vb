Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports SublimationInventory.Models
Imports SublimationInventory.Services
Imports SublimationInventory.UI

Namespace Forms
    Public Class LoginForm
        Inherits Form

        Public Property AuthenticatedUser As User

        Private txtUser As TextBox
        Private txtPass As TextBox
        Private lblError As Label

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "RJA Sportswear Inventory - Sign In"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.FormBorderStyle = FormBorderStyle.FixedSingle
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.ClientSize = New Size(800, 500)
            Me.BackColor = Theme.ContentBg
            Me.Font = Theme.AppFont(10.0F)
            Dim ico = Branding.CreateWindowIcon()
            If ico IsNot Nothing Then Me.Icon = ico

            Dim brand As New Panel With {.Dock = DockStyle.Left, .Width = 340, .BackColor = Theme.SidebarBg}
            AddHandler brand.Paint, AddressOf PaintBrand
            Me.Controls.Add(brand)

            Dim rightPane As New Panel With {.Dock = DockStyle.Fill, .BackColor = Theme.ContentBg}
            Me.Controls.Add(rightPane)
            rightPane.BringToFront()

            Dim title As New Label With {.Text = "Welcome back", .Font = Theme.AppFont(18.0F, FontStyle.Bold),
                                         .ForeColor = Theme.TextDark, .AutoSize = True, .Location = New Point(48, 76)}
            Dim subtitle As New Label With {.Text = "Sign in to manage your inventory", .Font = Theme.AppFont(10.0F),
                                            .ForeColor = Theme.TextMuted, .AutoSize = True, .Location = New Point(48, 112)}
            rightPane.Controls.Add(title)
            rightPane.Controls.Add(subtitle)

            rightPane.Controls.Add(New Label With {.Text = "Username", .ForeColor = Theme.TextDark, .AutoSize = True, .Location = New Point(48, 168)})
            txtUser = New TextBox With {.Location = New Point(48, 192), .Width = 340, .Font = Theme.AppFont(11.0F),
                                        .BorderStyle = BorderStyle.FixedSingle, .BackColor = Theme.CardBg}
            rightPane.Controls.Add(txtUser)

            rightPane.Controls.Add(New Label With {.Text = "Password", .ForeColor = Theme.TextDark, .AutoSize = True, .Location = New Point(48, 236)})
            txtPass = New TextBox With {.Location = New Point(48, 260), .Width = 340, .Font = Theme.AppFont(11.0F),
                                        .BorderStyle = BorderStyle.FixedSingle, .BackColor = Theme.CardBg,
                                        .UseSystemPasswordChar = True}
            rightPane.Controls.Add(txtPass)

            lblError = New Label With {.ForeColor = Theme.DangerRed, .AutoSize = True, .Location = New Point(48, 300), .Text = ""}
            rightPane.Controls.Add(lblError)

            Dim btnLogin As New Button With {.Text = "Sign In", .Location = New Point(48, 328), .Width = 340}
            UiHelpers.StyleAccentButton(btnLogin)
            AddHandler btnLogin.Click, AddressOf OnLogin
            rightPane.Controls.Add(btnLogin)
            Me.AcceptButton = btnLogin
        End Sub

        Private Sub PaintBrand(sender As Object, e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim panel = DirectCast(sender, Panel)

            ' --- Company logo, centred on the navy panel ---
            Dim d = 132
            Dim cx = (panel.Width - d) \ 2
            Dim cy = 62
            Branding.DrawCircular(g, cx, cy, d, Theme.Accent, 3.0F, "RJA")

            ' --- Wordmark ---
            Using f = Theme.AppFont(21.0F, FontStyle.Bold)
                TextRenderer.DrawText(g, "RJA", f, New Rectangle(0, cy + d + 22, panel.Width, 30),
                                      Theme.TextLight, TextFormatFlags.HorizontalCenter Or TextFormatFlags.NoPrefix)
            End Using
            Using f = Theme.AppFont(13.0F, FontStyle.Bold)
                TextRenderer.DrawText(g, "SPORTSWEAR", f, New Rectangle(0, cy + d + 54, panel.Width, 22),
                                      Theme.Accent, TextFormatFlags.HorizontalCenter Or TextFormatFlags.NoPrefix)
            End Using

            ' --- Divider + tagline ---
            Dim lineY = cy + d + 92
            Using p As New Pen(Color.FromArgb(70, Theme.Accent), 1.0F)
                g.DrawLine(p, 56, lineY, panel.Width - 56, lineY)
            End Using
            Using f = Theme.AppFont(9.0F)
                TextRenderer.DrawText(g, "Inventory Management System", f,
                                      New Rectangle(0, lineY + 14, panel.Width, 20),
                                      Theme.TextOnNavy, TextFormatFlags.HorizontalCenter Or TextFormatFlags.NoPrefix)
            End Using
        End Sub

        Private Sub OnLogin(sender As Object, e As EventArgs)
            lblError.Text = ""
            Dim u = txtUser.Text.Trim()
            Dim p = txtPass.Text
            If u = "" OrElse p = "" Then
                lblError.Text = "Please enter a username and password."
                Return
            End If

            Dim user = AuthService.Authenticate(u, p)
            If user Is Nothing Then
                lblError.Text = "Invalid username or password."
                txtPass.Clear()
                txtPass.Focus()
                Return
            End If

            AuthenticatedUser = user
            Me.DialogResult = DialogResult.OK
            Me.Close()
        End Sub
    End Class
End Namespace