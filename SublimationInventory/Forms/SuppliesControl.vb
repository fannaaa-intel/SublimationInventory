Imports System.Drawing
Imports System.Windows.Forms
Imports SublimationInventory.Data
Imports SublimationInventory.Models
Imports SublimationInventory.Services
Imports SublimationInventory.UI
Imports System.Drawing.Drawing2D

Namespace Forms
    Public Class SuppliesControl
        Inherits UserControl

        Private grid As DataGridView

        Public Sub New()
            Me.BackColor = Theme.ContentBg
            Me.Padding = New Padding(28, 22, 28, 22)
            Build()
            LoadData()
        End Sub

        Private Sub Build()
            Dim root As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 3, .BackColor = Theme.ContentBg}
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 46))
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 52))
            root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            Me.Controls.Add(root)

            root.Controls.Add(New Label With {.Text = "Supplies", .Font = Theme.AppFont(20.0F, FontStyle.Bold),
                                              .ForeColor = Theme.TextDark, .AutoSize = True}, 0, 0)

            Dim bar As New Panel With {.Dock = DockStyle.Fill, .BackColor = Theme.ContentBg}
            Dim btnAdd As New Button With {.Text = "+  Add Item", .Location = New Point(0, 8), .Width = 130}
            UiHelpers.StyleAccentButton(btnAdd)
            AddHandler btnAdd.Click, AddressOf OnAdd
            Dim btnEdit As New Button With {.Text = "Edit", .Location = New Point(140, 8), .Width = 100}
            UiHelpers.StyleSecondaryButton(btnEdit)
            AddHandler btnEdit.Click, AddressOf OnEdit
            Dim btnDelete As New Button With {.Text = "Delete", .Location = New Point(248, 8), .Width = 100}
            UiHelpers.StyleSecondaryButton(btnDelete)
            AddHandler btnDelete.Click, AddressOf OnDelete
            Dim btnRefresh As New Button With {.Text = "Refresh", .Location = New Point(356, 8), .Width = 100}
            UiHelpers.StyleSecondaryButton(btnRefresh)
            AddHandler btnRefresh.Click, Sub(s, e) LoadData()
            bar.Controls.AddRange(New Control() {btnAdd, btnEdit, btnDelete, btnRefresh})
            bar.Controls.Add(New Label With {.Text = "Rows highlighted amber are at or below their reorder level.",
                                             .ForeColor = Theme.TextMuted, .Font = Theme.AppFont(8.5F), .AutoSize = True, .Location = New Point(470, 18)})
            root.Controls.Add(bar, 0, 1)

            Dim card As New RoundedPanel With {.Dock = DockStyle.Fill, .FillColor = Theme.CardBg, .BackColor = Theme.ContentBg, .Padding = New Padding(10)}
            grid = New DataGridView With {.Dock = DockStyle.Fill}
            UiHelpers.StyleGrid(grid)
            grid.Columns.Add("Name", "Item Name")
            grid.Columns.Add("Category", "Category")
            grid.Columns.Add("Unit", "Unit")
            grid.Columns.Add("OnHand", "On Hand")
            grid.Columns.Add("Reorder", "Reorder At")
            grid.Columns.Add("Cost", "Unit Cost")
            grid.Columns.Add("Status", "Status")
            AddHandler grid.CellDoubleClick, Sub(s, e) OnEdit(s, e)
            card.Controls.Add(grid)
            root.Controls.Add(card, 0, 2)
        End Sub

        Private Sub LoadData()
            grid.Rows.Clear()
            For Each s In InventoryService.GetItemsWithStock()   ' shared computed on-hand
                Dim idx = grid.Rows.Add(s.Item.Name, s.Item.Category, s.Item.Unit, s.OnHand,
                               s.Item.ReorderThreshold, UiHelpers.Money(s.Item.UnitCost),
                                        If(s.IsLowStock, "LOW", "OK"))
                grid.Rows(idx).Tag = s.Item.ItemId
                If s.IsLowStock Then
                    grid.Rows(idx).DefaultCellStyle.BackColor = Theme.LowStockBg
                    grid.Rows(idx).DefaultCellStyle.ForeColor = Theme.LowStockText
                    grid.Rows(idx).DefaultCellStyle.SelectionBackColor = Theme.Highlight
                    grid.Rows(idx).DefaultCellStyle.SelectionForeColor = Color.White
                    grid.Rows(idx).Cells("Status").Style.Font = Theme.AppFont(9.5F, FontStyle.Bold)
                End If
            Next
        End Sub

        Private Function SelectedItemId() As Integer
            If grid.CurrentRow Is Nothing OrElse grid.CurrentRow.Tag Is Nothing Then Return 0
            Return Convert.ToInt32(grid.CurrentRow.Tag)
        End Function

        Private Sub OnAdd(sender As Object, e As EventArgs)
            Using dlg As New ItemEditDialog(Nothing)
                If dlg.ShowDialog() = DialogResult.OK Then
                    ItemRepository.Insert(dlg.Result)
                    LoadData()
                End If
            End Using
        End Sub

        Private Sub OnEdit(sender As Object, e As EventArgs)
            Dim id = SelectedItemId()
            If id = 0 Then Warn("Select an item to edit.") : Return
            Dim item = ItemRepository.GetById(id)
            If item Is Nothing Then Return
            Using dlg As New ItemEditDialog(item)
                If dlg.ShowDialog() = DialogResult.OK Then
                    ItemRepository.Update(dlg.Result)
                    LoadData()
                End If
            End Using
        End Sub

        Private Sub OnDelete(sender As Object, e As EventArgs)
            Dim id = SelectedItemId()
            If id = 0 Then Warn("Select an item to delete.") : Return
            Dim item = ItemRepository.GetById(id)
            If item Is Nothing Then Return

            If Not AppModal.Confirm(Me, "Delete item?",
                    $"""{item.Name}"" will be permanently removed from your supplies. This can't be undone.",
                    "Delete", danger:=True) Then Return

            If Not ItemRepository.Delete(id) Then
                AppModal.Warn(Me, "This item has stock transactions and cannot be deleted (its history must stay intact).", "Cannot delete")
                Return
            End If
            LoadData()
        End Sub

        Private Sub Warn(msg As String)
            AppModal.Warn(Me, msg, "Supplies")
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.Name = "SuppliesControl"
            Me.ResumeLayout(False)
        End Sub

        Private Sub SuppliesControl_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        End Sub
    End Class

    ''' <summary>Modal add/edit dialog for a single item — custom-chromed, draggable, rounded.</summary>
    Public Class ItemEditDialog
        Inherits Form

        Public Property Result As Item
        Private ReadOnly _editing As Item
        Private txtName As TextBox
        Private cmbCategory, cmbUnit As ComboBox
        Private numReorder As NumericUpDown
        Private numCost As NumericUpDown

        Private _drag As Boolean
        Private _dragStartScreen As Point
        Private _formStart As Point

        Public Sub New(existing As Item)
            _editing = existing
            Build()
            If existing IsNot Nothing Then LoadExisting(existing)
        End Sub

        Private Sub Build()
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.None
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.ClientSize = New Size(440, 430)
            Me.BackColor = Color.White
            Me.Font = Theme.AppFont(10.0F)
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer, True)

            ' ---------- Header ----------
            Dim header As New Panel With {.Dock = DockStyle.Top, .Height = 70, .BackColor = Theme.SidebarBg}
            AddHandler header.Paint, Sub(s, e)
                                         Using b As New SolidBrush(Theme.Highlight)   ' blue accent strip
                                             e.Graphics.FillRectangle(b, 0, header.Height - 3, header.Width, 3)
                                         End Using
                                     End Sub
            AddHandler header.MouseDown, AddressOf DragDown
            AddHandler header.MouseMove, AddressOf DragMove
            AddHandler header.MouseUp, Sub(s, e) _drag = False
            Me.Controls.Add(header)

            Dim title As New Label With {.Text = If(_editing Is Nothing, "Add Item", "Edit Item"),
                                         .ForeColor = Color.White, .Font = Theme.AppFont(15.0F, FontStyle.Bold),
                                         .AutoSize = True, .BackColor = Color.Transparent, .Location = New Point(24, 13)}
            Dim subtitle As New Label With {.Text = If(_editing Is Nothing, "Create a new supply item", "Update this supply item"),
                                            .ForeColor = Theme.TextMuted, .Font = Theme.AppFont(9.0F),
                                            .AutoSize = True, .BackColor = Color.Transparent, .Location = New Point(24, 41)}
            AddHandler title.MouseDown, AddressOf DragDown
            AddHandler title.MouseMove, AddressOf DragMove
            AddHandler title.MouseUp, Sub(s, e) _drag = False
            header.Controls.Add(title)
            header.Controls.Add(subtitle)

            Dim btnClose As New Button With {.Text = "✕", .FlatStyle = FlatStyle.Flat, .ForeColor = Color.White,
                                             .Size = New Size(34, 30), .Location = New Point(Me.ClientSize.Width - 46, 16),
                                             .BackColor = Theme.SidebarBg, .Font = Theme.AppFont(11.0F), .Cursor = Cursors.Hand,
                                             .DialogResult = DialogResult.Cancel, .TabStop = False,
                                             .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
            btnClose.FlatAppearance.BorderSize = 0
            btnClose.FlatAppearance.MouseOverBackColor = Theme.SidebarBgDarker
            header.Controls.Add(btnClose)

            ' ---------- Body ----------
            Dim y = 92
            AddLabel("ITEM NAME", 24, y) : y += 21
            txtName = New TextBox With {.Location = New Point(24, y), .Width = 392, .BorderStyle = BorderStyle.FixedSingle, .Font = Theme.AppFont(10.5F)}
            StyleField(txtName) : Me.Controls.Add(txtName) : y += 42

            AddLabel("CATEGORY", 24, y) : y += 21
            cmbCategory = New ComboBox With {.Location = New Point(24, y), .Width = 392, .Font = Theme.AppFont(10.5F), .FlatStyle = FlatStyle.Flat, .DropDownStyle = ComboBoxStyle.DropDown}
            cmbCategory.Items.AddRange(New Object() {"Blank Shirt", "Blank Bag", "Ink", "Transfer Paper", "Mug Blank", "Other"})
            StyleField(cmbCategory) : Me.Controls.Add(cmbCategory) : y += 42

            AddLabel("UNIT", 24, y) : y += 21
            cmbUnit = New ComboBox With {.Location = New Point(24, y), .Width = 186, .Font = Theme.AppFont(10.5F), .FlatStyle = FlatStyle.Flat, .DropDownStyle = ComboBoxStyle.DropDown}
            cmbUnit.Items.AddRange(New Object() {"pcs", "roll", "liter", "ml", "box", "set"})
            StyleField(cmbUnit) : Me.Controls.Add(cmbUnit) : y += 42

            AddLabel("REORDER THRESHOLD", 24, y)
            AddLabel("UNIT COST (" & UiHelpers.Peso & ")", 230, y) : y += 21
            numReorder = New NumericUpDown With {.Location = New Point(24, y), .Width = 186, .Minimum = 0, .Maximum = 1000000, .Font = Theme.AppFont(10.5F), .BorderStyle = BorderStyle.FixedSingle}
            numCost = New NumericUpDown With {.Location = New Point(230, y), .Width = 186, .Minimum = 0, .Maximum = 1000000, .DecimalPlaces = 2, .Increment = 0.25D, .Font = Theme.AppFont(10.5F), .BorderStyle = BorderStyle.FixedSingle}
            StyleField(numReorder) : StyleField(numCost)
            Me.Controls.Add(numReorder) : Me.Controls.Add(numCost)

            ' ---------- Footer ----------
            Dim footer As New Panel With {.Dock = DockStyle.Bottom, .Height = 64, .BackColor = Color.White}
            AddHandler footer.Paint, Sub(s, e)
                                         Using p As New Pen(Theme.BorderStrong, 1.5F)
                                             e.Graphics.DrawLine(p, 0, 0, footer.Width, 0)
                                         End Using
                                     End Sub
            Dim btnOk As New Button With {.Text = "Save", .Size = New Size(100, 38), .Location = New Point(208, 13), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
            UiHelpers.StyleAccentButton(btnOk)
            AddHandler btnOk.Click, AddressOf OnSave
            Dim btnCancel As New Button With {.Text = "Cancel", .Size = New Size(92, 38), .Location = New Point(316, 13), .DialogResult = DialogResult.Cancel, .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
            UiHelpers.StyleSecondaryButton(btnCancel)
            footer.Controls.Add(btnOk)
            footer.Controls.Add(btnCancel)
            Me.Controls.Add(footer)

            Me.AcceptButton = btnOk
            Me.CancelButton = btnCancel
        End Sub

        Private Sub AddLabel(text As String, x As Integer, y As Integer)
            Me.Controls.Add(New Label With {.Text = text, .AutoSize = True, .ForeColor = Theme.TextMuted,
                                            .Font = Theme.AppFont(8.0F, FontStyle.Bold), .Location = New Point(x, y)})
        End Sub

        ' Subtle focus feedback so fields feel alive.
        Private Sub StyleField(c As Control)
            c.BackColor = Color.White
            AddHandler c.Enter, Sub() c.BackColor = Color.FromArgb(244, 248, 253)
            AddHandler c.Leave, Sub() c.BackColor = Color.White
        End Sub

        Private Sub DragDown(s As Object, e As MouseEventArgs)
            If e.Button = MouseButtons.Left Then
                _drag = True
                _dragStartScreen = Cursor.Position
                _formStart = Me.Location
            End If
        End Sub

        Private Sub DragMove(s As Object, e As MouseEventArgs)
            If _drag Then
                Me.Location = New Point(_formStart.X + (Cursor.Position.X - _dragStartScreen.X),
                                        _formStart.Y + (Cursor.Position.Y - _dragStartScreen.Y))
            End If
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
            Using p As New Pen(Theme.BorderColor)
                Using path = Theme.RoundedRect(New Rectangle(0, 0, Width - 1, Height - 1), 14)
                    e.Graphics.DrawPath(p, path)
                End Using
            End Using
        End Sub

        Private Sub LoadExisting(item As Item)
            txtName.Text = item.Name
            cmbCategory.Text = item.Category
            cmbUnit.Text = item.Unit
            numReorder.Value = Math.Max(0, item.ReorderThreshold)
            numCost.Value = item.UnitCost
        End Sub

        Private Sub OnSave(sender As Object, e As EventArgs)
            If txtName.Text.Trim() = "" Then
                AppModal.Warn(Me, "Item name is required.", "Validation")
                Return
            End If
            Result = New Item With {
                .ItemId = If(_editing IsNot Nothing, _editing.ItemId, 0),
                .Name = txtName.Text.Trim(),
                .Category = cmbCategory.Text.Trim(),
                .Unit = cmbUnit.Text.Trim(),
                .ReorderThreshold = CInt(numReorder.Value),
                .UnitCost = numCost.Value}
            Me.DialogResult = DialogResult.OK
            Me.Close()
        End Sub
    End Class
    ''' <summary>Reusable themed Yes/No dialog. Blue for normal, red for destructive.</summary>
    Public Class ConfirmDialog
        Inherits Form

        Private _drag As Boolean
        Private _dragStartScreen As Point
        Private _formStart As Point

        Public Sub New(heading As String, message As String, confirmText As String, danger As Boolean)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.None
            Me.ClientSize = New Size(400, 200)
            Me.BackColor = Color.White
            Me.Font = Theme.AppFont(10.0F)
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer, True)

            Dim accent = If(danger, Color.FromArgb(200, 55, 45), Theme.Highlight)

            Me.Controls.Add(New Panel With {.Dock = DockStyle.Top, .Height = 6, .BackColor = accent})

            Dim icon As New Panel With {.Size = New Size(46, 46), .Location = New Point(28, 36), .BackColor = Color.White}
            AddHandler icon.Paint, Sub(s, e)
                                       e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                       Using b As New SolidBrush(Color.FromArgb(28, accent))
                                           e.Graphics.FillEllipse(b, 0, 0, 45, 45)
                                       End Using
                                       Using f = Theme.AppFont(22.0F, FontStyle.Bold)
                                           TextRenderer.DrawText(e.Graphics, If(danger, "!", "?"), f,
                                               New Rectangle(0, 0, 46, 46), accent,
                                               TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
                                       End Using
                                   End Sub
            Me.Controls.Add(icon)

            Me.Controls.Add(New Label With {.Text = heading, .Font = Theme.AppFont(13.0F, FontStyle.Bold),
                                            .ForeColor = Theme.TextDark, .AutoSize = True, .Location = New Point(90, 34)})
            Me.Controls.Add(New Label With {.Text = message, .Font = Theme.AppFont(9.5F),
                                            .ForeColor = Theme.TextMuted, .Location = New Point(90, 62), .Size = New Size(282, 62)})

            Dim btnConfirm As New Button With {.Text = confirmText, .Size = New Size(112, 38), .Location = New Point(170, 144),
                                               .FlatStyle = FlatStyle.Flat, .BackColor = accent, .ForeColor = Color.White,
                                               .Font = Theme.AppFont(10.0F, FontStyle.Bold), .Cursor = Cursors.Hand, .DialogResult = DialogResult.Yes}
            btnConfirm.FlatAppearance.BorderSize = 0
            Dim btnCancel As New Button With {.Text = "Cancel", .Size = New Size(90, 38), .Location = New Point(288, 144), .DialogResult = DialogResult.No}
            UiHelpers.StyleSecondaryButton(btnCancel)
            Me.Controls.Add(btnConfirm)
            Me.Controls.Add(btnCancel)
            Me.AcceptButton = btnConfirm
            Me.CancelButton = btnCancel

            AddHandler Me.MouseDown, AddressOf DragDown
            AddHandler Me.MouseMove, AddressOf DragMove
            AddHandler Me.MouseUp, Sub(s, e) _drag = False
        End Sub

        Private Sub DragDown(s As Object, e As MouseEventArgs)
            If e.Button = MouseButtons.Left Then
                _drag = True : _dragStartScreen = Cursor.Position : _formStart = Me.Location
            End If
        End Sub
        Private Sub DragMove(s As Object, e As MouseEventArgs)
            If _drag Then Me.Location = New Point(_formStart.X + (Cursor.Position.X - _dragStartScreen.X),
                                                  _formStart.Y + (Cursor.Position.Y - _dragStartScreen.Y))
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
            Using p As New Pen(Theme.BorderColor)
                Using path = Theme.RoundedRect(New Rectangle(0, 0, Width - 1, Height - 1), 14)
                    e.Graphics.DrawPath(p, path)
                End Using
            End Using
        End Sub
    End Class
End Namespace
