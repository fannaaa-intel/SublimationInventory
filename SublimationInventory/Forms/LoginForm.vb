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
            Me.Text = "Sublimation Inventory - Sign In"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.FormBorderStyle = FormBorderStyle.FixedSingle
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.ClientSize = New Size(780, 470)
            Me.BackColor = Theme.ContentBg
            Me.Font = Theme.AppFont(10.0F)

            Dim brand As New Panel With {.Dock = DockStyle.Left, .Width = 330, .BackColor = Theme.SidebarBg}
            AddHandler brand.Paint, AddressOf PaintBrand
            Me.Controls.Add(brand)

            Dim rightPane As New Panel With {.Dock = DockStyle.Fill, .BackColor = Theme.ContentBg}
            Me.Controls.Add(rightPane)
            rightPane.BringToFront()

            Dim title As New Label With {.Text = "Welcome back", .Font = Theme.AppFont(18.0F, FontStyle.Bold),
                                         .ForeColor = Theme.TextDark, .AutoSize = True, .Location = New Point(48, 62)}
            Dim subtitle As New Label With {.Text = "Sign in to manage your inventory", .Font = Theme.AppFont(10.0F),
                                            .ForeColor = Theme.TextMuted, .AutoSize = True, .Location = New Point(48, 98)}
            rightPane.Controls.Add(title)
            rightPane.Controls.Add(subtitle)

            rightPane.Controls.Add(New Label With {.Text = "Username", .ForeColor = Theme.TextDark, .AutoSize = True, .Location = New Point(48, 150)})
            txtUser = New TextBox With {.Location = New Point(48, 174), .Width = 340, .Font = Theme.AppFont(11.0F),
                                        .BorderStyle = BorderStyle.FixedSingle, .BackColor = Theme.CardBg}
            rightPane.Controls.Add(txtUser)

            rightPane.Controls.Add(New Label With {.Text = "Password", .ForeColor = Theme.TextDark, .AutoSize = True, .Location = New Point(48, 214)})
            txtPass = New TextBox With {.Location = New Point(48, 238), .Width = 340, .Font = Theme.AppFont(11.0F),
                                        .BorderStyle = BorderStyle.FixedSingle, .BackColor = Theme.CardBg,
                                        .UseSystemPasswordChar = True}
            rightPane.Controls.Add(txtPass)

            lblError = New Label With {.ForeColor = Theme.DangerRed, .AutoSize = True, .Location = New Point(48, 276), .Text = ""}
            rightPane.Controls.Add(lblError)

            Dim btnLogin As New Button With {.Text = "Sign In", .Location = New Point(48, 302), .Width = 340}
            UiHelpers.StyleAccentButton(btnLogin)
            AddHandler btnLogin.Click, AddressOf OnLogin
            rightPane.Controls.Add(btnLogin)
            Me.AcceptButton = btnLogin
        End Sub

        Private Sub PaintBrand(sender As Object, e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim d = 72
            Using ring As New Pen(Theme.Accent, 2.5F)
                g.DrawEllipse(ring, 52, 66, d + 8, d + 8)
            End Using
            Using b As New SolidBrush(Theme.Accent)
                g.FillEllipse(b, 56, 70, d, d)
            End Using
            Using f = Theme.AppFont(28.0F, FontStyle.Bold)
                TextRenderer.DrawText(g, "S", f, New Rectangle(56, 70, d, d), Color.White,
                                      TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
            End Using
            Using f = Theme.AppFont(17.0F, FontStyle.Bold)
                TextRenderer.DrawText(g, "Sublimation", f, New Point(54, 168), Theme.TextLight)
                TextRenderer.DrawText(g, "Inventory", f, New Point(54, 196), Theme.Accent)
            End Using
            Using f = Theme.AppFont(9.5F)
                ' NoPrefix: without it TextRenderer eats the "&" and underlines the next letter.
                TextRenderer.DrawText(g, "Track blanks, ink, paper & prints", f,
                                      New Rectangle(56, 232, 250, 20), Theme.TextOnNavy, TextFormatFlags.NoPrefix)
            End Using
            Using p As New Pen(Color.FromArgb(60, Theme.Accent), 1.0F)
                g.DrawLine(p, 56, 262, 260, 262)
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