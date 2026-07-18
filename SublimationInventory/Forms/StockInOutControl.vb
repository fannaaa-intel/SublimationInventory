Imports System.Drawing
Imports System.Windows.Forms
Imports SublimationInventory.Data
Imports SublimationInventory.Models
Imports SublimationInventory.Services
Imports SublimationInventory.UI

Namespace Forms
    Public Class StockInOutControl
        Inherits UserControl

        Private cmbInItem, cmbOutItem, cmbFilterItem, cmbOutReason As ComboBox
        Private numInQty, numOutQty As NumericUpDown
        Private dtInDate, dtOutDate, dtFrom, dtTo As DateTimePicker
        Private txtInSource, txtInNotes, txtOutNotes As TextBox
        Private grid As DataGridView

        Private inPanel, outPanel As Panel
        Private tabInBtn, tabOutBtn As Button
        Private indicator As Panel

        Public Sub New()
            Me.BackColor = Theme.ContentBg
            Me.Padding = New Padding(28, 22, 28, 22)
            Build()
            LoadItemsIntoCombos()
            LoadHistory()
        End Sub

        Private Sub Build()
            Dim root As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 4, .BackColor = Theme.ContentBg}
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 46))
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 272))
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 70))
            root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            Me.Controls.Add(root)

            root.Controls.Add(New Label With {.Text = "Stock In / Out", .Font = Theme.AppFont(20.0F, FontStyle.Bold),
                                              .ForeColor = Theme.TextDark, .AutoSize = True}, 0, 0)

            ' ---------- Entry card with underline-style tabs ----------
            Dim card As New RoundedPanel With {.Dock = DockStyle.Fill, .FillColor = Theme.CardBg, .BackColor = Theme.ContentBg, .Padding = New Padding(2)}
            root.Controls.Add(card, 0, 1)

            Dim strip As New Panel With {.Dock = DockStyle.Top, .Height = 50, .BackColor = Theme.CardBg}
            AddHandler strip.Paint, Sub(s, e)
                                        Using p As New Pen(Theme.BorderStrong)
                                            e.Graphics.DrawLine(p, 12, strip.Height - 1, strip.Width - 12, strip.Height - 1)
                                        End Using
                                    End Sub
            card.Controls.Add(strip)

            tabInBtn = MakeTab("Stock In", 16)
            tabOutBtn = MakeTab("Stock Out", 152)
            AddHandler tabInBtn.Click, Sub(s, e) SelectTab(True)
            AddHandler tabOutBtn.Click, Sub(s, e) SelectTab(False)
            strip.Controls.Add(tabInBtn)
            strip.Controls.Add(tabOutBtn)
            indicator = New Panel With {.Height = 3, .BackColor = Theme.Accent}
            strip.Controls.Add(indicator)
            indicator.BringToFront()

            Dim content As New Panel With {.Dock = DockStyle.Fill, .BackColor = Theme.CardBg}
            card.Controls.Add(content)
            content.BringToFront()

            inPanel = New Panel With {.Dock = DockStyle.Fill, .BackColor = Theme.CardBg}
            outPanel = New Panel With {.Dock = DockStyle.Fill, .BackColor = Theme.CardBg}
            content.Controls.Add(inPanel)
            content.Controls.Add(outPanel)
            BuildInTab(inPanel)
            BuildOutTab(outPanel)

            ' ---------- Filter card ----------
            root.Controls.Add(BuildFilterBar(), 0, 2)

            ' ---------- History grid card ----------
            Dim gridCard As New RoundedPanel With {.Dock = DockStyle.Fill, .FillColor = Theme.CardBg, .BackColor = Theme.ContentBg, .Padding = New Padding(10)}
            grid = New DataGridView With {.Dock = DockStyle.Fill}
            UiHelpers.StyleGrid(grid)
            grid.Columns.Add("Date", "Date")
            grid.Columns.Add("Item", "Item")
            grid.Columns.Add("Type", "Type")
            grid.Columns.Add("Qty", "Qty")
            grid.Columns.Add("Detail", "Source / Reason")
            grid.Columns.Add("Notes", "Notes")
            gridCard.Controls.Add(grid)
            root.Controls.Add(gridCard, 0, 3)

            SelectTab(True)
        End Sub

        Private Function MakeTab(text As String, x As Integer) As Button
            Dim b As New Button With {.Text = text, .FlatStyle = FlatStyle.Flat, .Location = New Point(x, 9),
                                      .Size = New Size(128, 34), .Font = Theme.AppFont(11.0F), .BackColor = Theme.CardBg,
                                      .ForeColor = Theme.TextMuted, .Cursor = Cursors.Hand, .TabStop = False}
            b.FlatAppearance.BorderSize = 0
            b.FlatAppearance.MouseOverBackColor = Theme.CardBg
            Return b
        End Function

        Private Sub SelectTab(showIn As Boolean)
            If inPanel Is Nothing OrElse outPanel Is Nothing Then Return
            inPanel.Visible = showIn
            outPanel.Visible = Not showIn
            ApplyTabStyle(tabInBtn, showIn)
            ApplyTabStyle(tabOutBtn, Not showIn)
            Dim active = If(showIn, tabInBtn, tabOutBtn)
            indicator.Width = active.Width
            indicator.Left = active.Left
            indicator.Top = active.Parent.Height - 3
        End Sub

        Private Sub ApplyTabStyle(b As Button, active As Boolean)
            b.ForeColor = If(active, Theme.Accent, Theme.TextMuted)
            b.Font = Theme.AppFont(11.0F, If(active, FontStyle.Bold, FontStyle.Regular))
        End Sub

        Private Sub BuildInTab(p As Panel)
            cmbInItem = FieldCombo(p, "ITEM", 22, 16)
            numInQty = FieldNumeric(p, "QUANTITY", 360, 16)
            dtInDate = FieldDate(p, "DATE", 560, 16)
            txtInSource = FieldText(p, "SOURCE / SUPPLIER (OPTIONAL)", 22, 86, 316)
            txtInNotes = FieldText(p, "NOTES", 360, 86, 380)
            Dim btn As New Button With {.Text = "Record Stock In", .Location = New Point(22, 168), .Width = 210, .Height = 40}
            UiHelpers.StyleAccentButton(btn)
            AddHandler btn.Click, AddressOf OnRecordIn
            p.Controls.Add(btn)
        End Sub

        Private Sub BuildOutTab(p As Panel)
            cmbOutItem = FieldCombo(p, "ITEM", 22, 16)
            numOutQty = FieldNumeric(p, "QUANTITY", 360, 16)
            dtOutDate = FieldDate(p, "DATE", 560, 16)
            AddFieldLabel(p, "REASON", 22, 86)
            cmbOutReason = New ComboBox With {.Width = 316, .Location = New Point(22, 108), .Font = Theme.AppFont(10.5F), .DropDownStyle = ComboBoxStyle.DropDown}
            cmbOutReason.Items.AddRange(New Object() {"Used in order", "Damaged", "Sample", "Adjustment", "Other"})
            cmbOutReason.SelectedIndex = 0
            StyleField(cmbOutReason)
            p.Controls.Add(cmbOutReason)
            txtOutNotes = FieldText(p, "NOTES", 360, 86, 380)
            Dim btn As New Button With {.Text = "Record Stock Out", .Location = New Point(22, 168), .Width = 210, .Height = 40}
            UiHelpers.StyleAccentButton(btn)
            AddHandler btn.Click, AddressOf OnRecordOut
            p.Controls.Add(btn)
        End Sub

        Private Function BuildFilterBar() As Control
            Dim card As New RoundedPanel With {.Dock = DockStyle.Fill, .FillColor = Theme.CardBg,
                                       .BackColor = Theme.ContentBg, .Padding = New Padding(20, 0, 22, 0)}

            Dim row As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 10, .RowCount = 1,
                                          .BackColor = Color.Transparent}
            ' History | From | dtFrom | To | dtTo | Item | combo | spacer | Apply | Reset
            row.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))         ' History
            row.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))         ' From
            row.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 104))    ' dtFrom
            row.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))         ' To
            row.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 104))    ' dtTo
            row.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))         ' Item
            row.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 180))    ' combo (owns full width incl. arrow)
            row.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))     ' spacer -> pushes buttons right
            row.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))         ' Apply
            row.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))         ' Reset
            card.Controls.Add(row)

            Dim lblHistory As New Label With {.Text = "History", .Font = Theme.AppFont(13.0F, FontStyle.Bold),
                                      .ForeColor = Theme.TextDark, .AutoSize = True, .Anchor = AnchorStyles.Left,
                                      .Margin = New Padding(0, 0, 22, 0), .BackColor = Color.Transparent}
            Dim lblFrom As New Label With {.Text = "From", .ForeColor = Theme.TextMuted, .AutoSize = True,
                                   .Anchor = AnchorStyles.Left, .Margin = New Padding(0, 0, 8, 0), .BackColor = Color.Transparent}
            dtFrom = New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 104,
                                      .Anchor = AnchorStyles.Left, .Margin = New Padding(0, 0, 18, 0),
                                      .Value = DateTime.Now.AddMonths(-1)}
            Dim lblTo As New Label With {.Text = "To", .ForeColor = Theme.TextMuted, .AutoSize = True,
                                 .Anchor = AnchorStyles.Left, .Margin = New Padding(0, 0, 8, 0), .BackColor = Color.Transparent}
            dtTo = New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 104,
                                    .Anchor = AnchorStyles.Left, .Margin = New Padding(0, 0, 18, 0), .Value = DateTime.Now}
            Dim lblItem As New Label With {.Text = "Item", .ForeColor = Theme.TextMuted, .AutoSize = True,
                                   .Anchor = AnchorStyles.Left, .Margin = New Padding(0, 0, 8, 0), .BackColor = Color.Transparent}
            cmbFilterItem = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 180,
                                       .Anchor = AnchorStyles.Left, .Margin = New Padding(0), .Font = Theme.AppFont(10.0F),
                                       .DrawMode = DrawMode.OwnerDrawFixed, .ItemHeight = 20}
            AddHandler cmbFilterItem.DrawItem, AddressOf DrawFilterItem

            Dim btnApply As New Button With {.Text = "Apply", .Anchor = AnchorStyles.Right, .Margin = New Padding(0, 0, 14, 0)}
            UiHelpers.StyleAccentButton(btnApply)
            btnApply.Size = New Size(92, 34)
            AddHandler btnApply.Click, Sub(s, e) LoadHistory()

            Dim btnReset As New Button With {.Text = "Reset", .Anchor = AnchorStyles.Right, .Margin = New Padding(0)}
            UiHelpers.StyleSecondaryButton(btnReset)
            btnReset.Size = New Size(84, 34)
            AddHandler btnReset.Click, Sub(s, e)
                                           dtFrom.Value = DateTime.Now.AddMonths(-1)
                                           dtTo.Value = DateTime.Now
                                           If cmbFilterItem.Items.Count > 0 Then cmbFilterItem.SelectedIndex = 0
                                           LoadHistory()
                                       End Sub

            row.Controls.Add(lblHistory, 0, 0)
            row.Controls.Add(lblFrom, 1, 0)
            row.Controls.Add(dtFrom, 2, 0)
            row.Controls.Add(lblTo, 3, 0)
            row.Controls.Add(dtTo, 4, 0)
            row.Controls.Add(lblItem, 5, 0)
            row.Controls.Add(cmbFilterItem, 6, 0)
            ' column 7 (spacer) intentionally empty
            row.Controls.Add(btnApply, 8, 0)
            row.Controls.Add(btnReset, 9, 0)

            Return card
        End Function

        Private Sub DrawFilterItem(sender As Object, e As DrawItemEventArgs)
            Dim cmb = DirectCast(sender, ComboBox)
            Dim inList = (e.State And DrawItemState.ComboBoxEdit) <> DrawItemState.ComboBoxEdit
            Dim selected = inList AndAlso (e.State And DrawItemState.Selected) = DrawItemState.Selected

            Using b As New SolidBrush(If(selected, Theme.Highlight, Color.White))
                e.Graphics.FillRectangle(b, e.Bounds)
            End Using
            If e.Index >= 0 Then
                Dim text = cmb.GetItemText(cmb.Items(e.Index))
                Dim fore = If(selected, Color.White, Theme.TextDark)
                Dim rect = New Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height)
                TextRenderer.DrawText(e.Graphics, text, cmb.Font, rect, fore,
                                      TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)
            End If
        End Sub

        ' ---- field helpers ----
        Private Sub AddFieldLabel(parent As Control, text As String, x As Integer, y As Integer)
            parent.Controls.Add(New Label With {.Text = text, .AutoSize = True, .ForeColor = Theme.TextMuted, .Font = Theme.AppFont(8.0F, FontStyle.Bold), .Location = New Point(x, y), .BackColor = Color.Transparent})
        End Sub
        Private Sub StyleField(c As Control)
            c.BackColor = Color.White
            AddHandler c.Enter, Sub() c.BackColor = Color.FromArgb(244, 248, 253)
            AddHandler c.Leave, Sub() c.BackColor = Color.White
        End Sub
        Private Function FieldCombo(parent As Control, label As String, x As Integer, y As Integer) As ComboBox
            AddFieldLabel(parent, label, x, y)
            Dim c As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 316, .Location = New Point(x, y + 22), .Font = Theme.AppFont(10.5F)}
            StyleField(c)
            parent.Controls.Add(c)
            Return c
        End Function
        Private Function FieldNumeric(parent As Control, label As String, x As Integer, y As Integer) As NumericUpDown
            AddFieldLabel(parent, label, x, y)
            Dim n As New NumericUpDown With {.Minimum = 1, .Maximum = 1000000, .Value = 1, .Width = 160, .Location = New Point(x, y + 22), .Font = Theme.AppFont(10.5F), .BorderStyle = BorderStyle.FixedSingle}
            StyleField(n)
            parent.Controls.Add(n)
            Return n
        End Function
        Private Function FieldDate(parent As Control, label As String, x As Integer, y As Integer) As DateTimePicker
            AddFieldLabel(parent, label, x, y)
            Dim d As New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 160, .Location = New Point(x, y + 22), .Font = Theme.AppFont(10.5F), .Value = DateTime.Now}
            parent.Controls.Add(d)
            Return d
        End Function
        Private Function FieldText(parent As Control, label As String, x As Integer, y As Integer, w As Integer) As TextBox
            AddFieldLabel(parent, label, x, y)
            Dim t As New TextBox With {.Width = w, .Location = New Point(x, y + 22), .Font = Theme.AppFont(10.5F), .BorderStyle = BorderStyle.FixedSingle}
            StyleField(t)
            parent.Controls.Add(t)
            Return t
        End Function

        ' ---- data ----
        Private Sub LoadItemsIntoCombos()
            Dim items = ItemRepository.GetAll()
            SetupItemCombo(cmbInItem, New List(Of Item)(items))
            SetupItemCombo(cmbOutItem, New List(Of Item)(items))
            Dim filterList As New List(Of Item) From {New Item With {.ItemId = 0, .Name = "All items"}}
            filterList.AddRange(items)
            cmbFilterItem.DisplayMember = "Name"
            cmbFilterItem.ValueMember = "ItemId"
            cmbFilterItem.DataSource = filterList
            cmbFilterItem.SelectedIndex = 0
        End Sub
        Private Sub SetupItemCombo(cmb As ComboBox, items As List(Of Item))
            cmb.DisplayMember = "Name"
            cmb.ValueMember = "ItemId"
            cmb.DataSource = items
        End Sub

        Private Sub OnRecordIn(sender As Object, e As EventArgs)
            If cmbInItem.SelectedValue Is Nothing Then
                Warn("Please select an item.") : Return
            End If
            Dim itemId = Convert.ToInt32(cmbInItem.SelectedValue)
            Dim qty = CInt(numInQty.Value)
            If qty <= 0 Then Warn("Quantity must be greater than zero.") : Return

            TransactionRepository.Insert(New StockTransaction With {
                .ItemId = itemId, .Type = "In", .Quantity = qty, .TransactionDate = dtInDate.Value,
                .Source = txtInSource.Text.Trim(), .Notes = txtInNotes.Text.Trim()})
            AppModal.Success(Me, "Stock in recorded.")
            txtInSource.Clear() : txtInNotes.Clear() : numInQty.Value = 1
            LoadHistory()
        End Sub

        Private Sub OnRecordOut(sender As Object, e As EventArgs)
            If cmbOutItem.SelectedValue Is Nothing Then
                Warn("Please select an item.") : Return
            End If
            Dim itemId = Convert.ToInt32(cmbOutItem.SelectedValue)
            Dim qty = CInt(numOutQty.Value)
            If qty <= 0 Then Warn("Quantity must be greater than zero.") : Return

            Dim onHand = InventoryService.GetOnHand(itemId)
            If qty > onHand Then
                AppModal.Warn(Me, $"Cannot remove {qty} unit(s) — only {onHand} on hand for this item.", "Not enough stock")
                Return
            End If

            TransactionRepository.Insert(New StockTransaction With {
                .ItemId = itemId, .Type = "Out", .Quantity = qty, .TransactionDate = dtOutDate.Value,
                .Reason = cmbOutReason.Text.Trim(), .Notes = txtOutNotes.Text.Trim()})
            AppModal.Success(Me, "Stock out recorded.")
            txtOutNotes.Clear() : numOutQty.Value = 1
            LoadHistory()
        End Sub

        Private Sub LoadHistory()
            If grid Is Nothing Then Return
            grid.Rows.Clear()
            Dim itemId As Integer? = Nothing
            If cmbFilterItem.SelectedValue IsNot Nothing AndAlso Convert.ToInt32(cmbFilterItem.SelectedValue) > 0 Then
                itemId = Convert.ToInt32(cmbFilterItem.SelectedValue)
            End If
            For Each t In TransactionRepository.Query(itemId:=itemId, fromDate:=dtFrom.Value, toDate:=dtTo.Value)
                Dim detail = If(t.Type = "In", t.Source, t.Reason)
                Dim idx = grid.Rows.Add(t.TransactionDate.ToString("MMM d, yyyy"), t.ItemName, t.Type, t.Quantity, detail, t.Notes)
                grid.Rows(idx).Cells("Type").Style.ForeColor = If(t.Type = "Out", Color.Firebrick, Color.SeaGreen)
            Next
        End Sub

        Private Sub Warn(msg As String)
            AppModal.Warn(Me, msg, "Validation")
        End Sub
        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.Name = "SuppliesControl"
            Me.ResumeLayout(False)
        End Sub
        Private Sub SuppliesControl_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        End Sub
    End Class
End Namespace