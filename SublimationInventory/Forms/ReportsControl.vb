Imports System.Drawing
Imports System.Windows.Forms
Imports SublimationInventory.Data
Imports SublimationInventory.Models
Imports SublimationInventory.Services
Imports SublimationInventory.UI

Namespace Forms
    Public Class ReportsControl
        Inherits UserControl

        Private gridLow, gridMove, gridValue As DataGridView
        Private cmbMoveItem, cmbMoveType As ComboBox
        Private dtMoveFrom, dtMoveTo As DateTimePicker
        Private lblGrandTotal As Label

        Private pages() As Panel
        Private tabBtns() As Button
        Private indicator As Panel

        Public Sub New()
            Me.BackColor = Theme.ContentBg
            Me.Padding = New Padding(28, 22, 28, 22)
            Build()
            LoadLowStock()
            LoadMovement()
            LoadValue()
        End Sub

        Private Sub Build()
            Dim root As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2, .BackColor = Theme.ContentBg}
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 46))
            root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            Me.Controls.Add(root)

            root.Controls.Add(New Label With {.Text = "Reports", .Font = Theme.AppFont(20.0F, FontStyle.Bold),
                                              .ForeColor = Theme.TextDark, .AutoSize = True}, 0, 0)

            ' ---------- Card shell with custom underline tabs ----------
            Dim card As New RoundedPanel With {.Dock = DockStyle.Fill, .FillColor = Theme.CardBg, .BackColor = Theme.ContentBg, .Padding = New Padding(2)}
            root.Controls.Add(card, 0, 1)

            Dim strip As New Panel With {.Dock = DockStyle.Top, .Height = 50, .BackColor = Theme.CardBg}
            AddHandler strip.Paint, Sub(s, e)
                                        Using p As New Pen(Theme.BorderStrong)
                                            e.Graphics.DrawLine(p, 12, strip.Height - 1, strip.Width - 12, strip.Height - 1)
                                        End Using
                                    End Sub
            card.Controls.Add(strip)

            Dim labels = New String() {"Low Stock", "Stock Movement", "Stock Value"}
            Dim widths = New Integer() {120, 160, 130}
            tabBtns = New Button(2) {}
            Dim x = 16
            For i = 0 To 2
                tabBtns(i) = MakeTab(labels(i), x, widths(i))
                Dim idx = i
                AddHandler tabBtns(i).Click, Sub(s, e) SelectTab(idx)
                strip.Controls.Add(tabBtns(i))
                x += widths(i) + 8
            Next
            indicator = New Panel With {.Height = 3, .BackColor = Theme.Accent}
            strip.Controls.Add(indicator)
            indicator.BringToFront()

            Dim content As New Panel With {.Dock = DockStyle.Fill, .BackColor = Theme.CardBg}
            card.Controls.Add(content)
            content.BringToFront()

            Dim pageLow As New Panel With {.Dock = DockStyle.Fill, .BackColor = Theme.CardBg, .Padding = New Padding(18, 14, 18, 18)}
            Dim pageMove As New Panel With {.Dock = DockStyle.Fill, .BackColor = Theme.CardBg, .Padding = New Padding(18, 14, 18, 18)}
            Dim pageValue As New Panel With {.Dock = DockStyle.Fill, .BackColor = Theme.CardBg, .Padding = New Padding(18, 14, 18, 18)}
            pages = New Panel() {pageLow, pageMove, pageValue}
            content.Controls.AddRange(pages)

            BuildLowTab(pageLow)
            BuildMoveTab(pageMove)
            BuildValueTab(pageValue)

            SelectTab(0)
        End Sub

        ' ---------------- custom tabs ----------------
        Private Function MakeTab(text As String, x As Integer, w As Integer) As Button
            Dim b As New Button With {.Text = text, .FlatStyle = FlatStyle.Flat, .Location = New Point(x, 9),
                                      .Size = New Size(w, 34), .Font = Theme.AppFont(11.0F), .BackColor = Theme.CardBg,
                                      .ForeColor = Theme.TextMuted, .Cursor = Cursors.Hand, .TabStop = False}
            b.FlatAppearance.BorderSize = 0
            b.FlatAppearance.MouseOverBackColor = Theme.CardBg
            Return b
        End Function

        Private Sub SelectTab(idx As Integer)
            If pages Is Nothing Then Return
            For i = 0 To pages.Length - 1
                pages(i).Visible = (i = idx)
                tabBtns(i).ForeColor = If(i = idx, Theme.Accent, Theme.TextMuted)
                tabBtns(i).Font = Theme.AppFont(11.0F, If(i = idx, FontStyle.Bold, FontStyle.Regular))
            Next
            indicator.Width = tabBtns(idx).Width
            indicator.Left = tabBtns(idx).Left
            indicator.Top = tabBtns(idx).Parent.Height - 3
        End Sub

        ' ---------------- toolbar helpers ----------------
        Private Function RightBar() As FlowLayoutPanel
            Return New FlowLayoutPanel With {.Dock = DockStyle.Right, .FlowDirection = FlowDirection.LeftToRight,
                                             .AutoSize = True, .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                                             .WrapContents = False, .BackColor = Color.Transparent, .Padding = New Padding(16, 6, 0, 0)}
        End Function

        Private Sub StyleField(c As Control)
            c.BackColor = Color.White
            AddHandler c.Enter, Sub() c.BackColor = Color.FromArgb(244, 248, 253)
            AddHandler c.Leave, Sub() c.BackColor = Color.White
        End Sub

        ' ---------------- Low stock ----------------
        Private Sub BuildLowTab(page As Panel)
            Dim bar As New Panel With {.Dock = DockStyle.Top, .Height = 44, .BackColor = Theme.CardBg}
            Dim right = RightBar()
            Dim btn As New Button With {.Text = "Export CSV", .Size = New Size(120, 34), .Margin = New Padding(0)}
            UiHelpers.StyleSecondaryButton(btn)
            AddHandler btn.Click, Sub(s, e) UiHelpers.ExportGridToCsv(gridLow, "low_stock_report.csv")
            right.Controls.Add(btn)
            bar.Controls.Add(right)
            bar.Controls.Add(New Label With {.Text = "Items at or below their reorder threshold. Click a column header to sort.",
                                             .ForeColor = Theme.TextMuted, .AutoSize = True, .Location = New Point(2, 12), .BackColor = Color.Transparent})

            gridLow = NewGrid()
            gridLow.Columns.Add("Name", "Item Name")
            gridLow.Columns.Add("Category", "Category")
            gridLow.Columns.Add("OnHand", "On Hand")
            gridLow.Columns.Add("Reorder", "Reorder At")
            gridLow.Columns.Add("Short", "Shortfall")
            For Each c As DataGridViewColumn In gridLow.Columns
                c.SortMode = DataGridViewColumnSortMode.Automatic
            Next

            page.Controls.Add(WrapInCard(gridLow))
            page.Controls.Add(bar)
        End Sub

        Private Sub LoadLowStock()
            gridLow.Rows.Clear()
            For Each s In InventoryService.GetLowStockItems()
                gridLow.Rows.Add(s.Item.Name, s.Item.Category, s.OnHand, s.Item.ReorderThreshold,
                                 Math.Max(0, s.Item.ReorderThreshold - s.OnHand))
            Next
            If gridLow.Rows.Count = 0 Then
                gridLow.Rows.Add("No items are low on stock.", "", "", "", "")
            End If
        End Sub

        ' ---------------- Stock movement ----------------
        Private Sub BuildMoveTab(page As Panel)
            ' Outer bar: 2 columns. col0 = flexible filters (wraps), col1 = buttons (reserved, right).
            Dim bar As New TableLayoutPanel With {.Dock = DockStyle.Top, .ColumnCount = 2, .RowCount = 1,
                                          .AutoSize = True, .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                                          .BackColor = Theme.CardBg, .Padding = New Padding(0, 4, 0, 6)}
            bar.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))   ' filters (shrinks -> wraps)
            bar.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))       ' buttons (always reserved)
            bar.RowStyles.Add(New RowStyle(SizeType.AutoSize))

            ' --- filters: wrapping flow; each label+field is one unit so pairs never split ---
            Dim filters As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.LeftToRight,
                                             .WrapContents = True, .AutoSize = True,
                                             .AutoSizeMode = AutoSizeMode.GrowAndShrink, .BackColor = Color.Transparent,
                                             .Margin = New Padding(0)}

            dtMoveFrom = New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 104,
                                          .Margin = New Padding(0), .Value = DateTime.Now.AddMonths(-3)}
            dtMoveTo = New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 104,
                                        .Margin = New Padding(0), .Value = DateTime.Now}

            cmbMoveType = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 88, .Margin = New Padding(0)}
            cmbMoveType.Items.AddRange(New Object() {"All", "In", "Out"})
            cmbMoveType.SelectedIndex = 0
            StyleField(cmbMoveType)

            cmbMoveItem = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 168, .Margin = New Padding(0)}
            Dim filterList As New List(Of Item) From {New Item With {.ItemId = 0, .Name = "All items"}}
            filterList.AddRange(ItemRepository.GetAll())
            cmbMoveItem.DisplayMember = "Name" : cmbMoveItem.ValueMember = "ItemId" : cmbMoveItem.DataSource = filterList
            StyleField(cmbMoveItem)

            filters.Controls.Add(MoveField("From", dtMoveFrom))
            filters.Controls.Add(MoveField("To", dtMoveTo))
            filters.Controls.Add(MoveField("Type", cmbMoveType))
            filters.Controls.Add(MoveField("Item", cmbMoveItem))

            ' normalize all filter fields to one height + font so rows line up
            For Each f As Control In New Control() {dtMoveFrom, dtMoveTo, cmbMoveType, cmbMoveItem}
                f.Font = Theme.AppFont(10.0F)
                f.Height = 26
            Next


            ' --- buttons: own column, never overlapped ---
            Dim buttons As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.LeftToRight,
                                             .WrapContents = False, .AutoSize = True,
                                             .AutoSizeMode = AutoSizeMode.GrowAndShrink, .BackColor = Color.Transparent,
                                             .Margin = New Padding(16, 0, 0, 0)}
            Dim btnApply As New Button With {.Text = "Apply", .Size = New Size(84, 34), .Margin = New Padding(0, 2, 10, 0)}
            UiHelpers.StyleAccentButton(btnApply)
            AddHandler btnApply.Click, Sub(s, e) LoadMovement()
            Dim btnExport As New Button With {.Text = "Export CSV", .Size = New Size(116, 34), .Margin = New Padding(0, 2, 0, 0)}
            UiHelpers.StyleSecondaryButton(btnExport)
            AddHandler btnExport.Click, Sub(s, e) UiHelpers.ExportGridToCsv(gridMove, "stock_movement_report.csv")
            buttons.Controls.Add(btnApply)
            buttons.Controls.Add(btnExport)

            bar.Controls.Add(filters, 0, 0)
            bar.Controls.Add(buttons, 1, 0)

            gridMove = NewGrid()
            gridMove.Columns.Add("Date", "Date")
            gridMove.Columns.Add("Item", "Item")
            gridMove.Columns.Add("Type", "Type")
            gridMove.Columns.Add("Qty", "Qty")
            gridMove.Columns.Add("Balance", "Running On-Hand")
            gridMove.Columns.Add("Detail", "Source / Reason")
            UiHelpers.SetMinColumnWidths(gridMove, 96, 150, 70, 60, 120, 150)

            page.Controls.Add(WrapInCard(gridMove))
            page.Controls.Add(bar)
        End Sub

        Private Function MoveField(text As String, field As Control) As Control
            Const LABEL_W As Integer = 44   '  <-- widen this to push all dropdowns further right
            Const ROW_GAP As Integer = 10   '  <-- vertical breathing space between wrapped lines

            Dim wrap As New TableLayoutPanel With {.ColumnCount = 2, .RowCount = 1, .AutoSize = True,
                                           .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                                           .Margin = New Padding(0, 0, 16, ROW_GAP), .Padding = New Padding(0),
                                           .BackColor = Color.Transparent}
            wrap.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, LABEL_W))   ' fixed -> fields align across rows
            wrap.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            wrap.RowStyles.Add(New RowStyle(SizeType.AutoSize))

            Dim lbl As New Label With {.Text = text, .ForeColor = Theme.TextMuted, .AutoSize = True,
                               .Anchor = AnchorStyles.Left, .Margin = New Padding(0),
                               .BackColor = Color.Transparent}
            field.Anchor = AnchorStyles.Left
            field.Margin = New Padding(0)

            wrap.Controls.Add(lbl, 0, 0)
            wrap.Controls.Add(field, 1, 0)
            Return wrap
        End Function

        Private Sub LoadMovement()
            gridMove.Rows.Clear()

            Dim balAfter As New Dictionary(Of Integer, Integer)()
            Dim running As New Dictionary(Of Integer, Integer)()
            Dim allAsc = TransactionRepository.Query()
            allAsc.Reverse()
            For Each t In allAsc
                Dim delta = If(t.Type = "In", t.Quantity, -t.Quantity)
                running(t.ItemId) = If(running.ContainsKey(t.ItemId), running(t.ItemId), 0) + delta
                balAfter(t.TransactionId) = running(t.ItemId)
            Next

            Dim itemId As Integer? = Nothing
            If cmbMoveItem.SelectedValue IsNot Nothing AndAlso Convert.ToInt32(cmbMoveItem.SelectedValue) > 0 Then
                itemId = Convert.ToInt32(cmbMoveItem.SelectedValue)
            End If
            Dim type As String = Nothing
            If cmbMoveType.SelectedIndex > 0 Then type = cmbMoveType.Text

            For Each t In TransactionRepository.Query(itemId:=itemId, type:=type, fromDate:=dtMoveFrom.Value, toDate:=dtMoveTo.Value)
                Dim detail = If(t.Type = "In", t.Source, t.Reason)
                Dim idx = gridMove.Rows.Add(t.TransactionDate.ToString("MMM d, yyyy"), t.ItemName, t.Type, t.Quantity,
                                            If(balAfter.ContainsKey(t.TransactionId), balAfter(t.TransactionId), 0), detail)
                gridMove.Rows(idx).Cells("Type").Style.ForeColor = If(t.Type = "Out", Color.Firebrick, Color.SeaGreen)
            Next
        End Sub

        ' ---------------- Stock value ----------------
        Private Sub BuildValueTab(page As Panel)
            Dim bar As New Panel With {.Dock = DockStyle.Top, .Height = 44, .BackColor = Theme.CardBg}
            Dim right = RightBar()
            Dim btn As New Button With {.Text = "Export CSV", .Size = New Size(120, 34), .Margin = New Padding(0)}
            UiHelpers.StyleSecondaryButton(btn)
            AddHandler btn.Click, Sub(s, e) UiHelpers.ExportGridToCsv(gridValue, "stock_value_report.csv")
            right.Controls.Add(btn)
            bar.Controls.Add(right)
            bar.Controls.Add(New Label With {.Text = "On-hand quantity × unit cost per item.",
                                             .ForeColor = Theme.TextMuted, .AutoSize = True, .Location = New Point(2, 12), .BackColor = Color.Transparent})

            ' footer pill with the grand total
            Dim footer As New Panel With {.Dock = DockStyle.Bottom, .Height = 54, .BackColor = Theme.CardBg, .Padding = New Padding(0, 8, 0, 0)}
            Dim pill As New RoundedPanel With {.Dock = DockStyle.Right, .Width = 340, .FillColor = Theme.SecondaryDark, .BackColor = Theme.CardBg, .DrawBorder = False}
            lblGrandTotal = New Label With {.Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleCenter,
                                            .Font = Theme.AppFont(12.0F, FontStyle.Bold), .ForeColor = Theme.TextLight, .BackColor = Color.Transparent}
            pill.Controls.Add(lblGrandTotal)
            footer.Controls.Add(pill)

            gridValue = NewGrid()
            gridValue.Columns.Add("Name", "Item Name")
            gridValue.Columns.Add("OnHand", "On Hand")
            gridValue.Columns.Add("Cost", "Unit Cost")
            gridValue.Columns.Add("Value", "Stock Value")

            page.Controls.Add(WrapInCard(gridValue))
            page.Controls.Add(footer)
            page.Controls.Add(bar)
        End Sub

        Private Sub LoadValue()
            gridValue.Rows.Clear()
            Dim total As Decimal = 0
            For Each s In InventoryService.GetItemsWithStock()
                gridValue.Rows.Add(s.Item.Name, s.OnHand, UiHelpers.Money(s.Item.UnitCost), UiHelpers.Money(s.StockValue))
                total += s.StockValue
            Next
            lblGrandTotal.Text = "Total inventory value:   " & UiHelpers.Money(total)
        End Sub

        ' ---------------- shared grid helpers ----------------
        Private Function NewGrid() As DataGridView
            Dim g As New DataGridView With {.Dock = DockStyle.Fill}
            UiHelpers.StyleGrid(g)
            Return g
        End Function

        Private Function WrapInCard(g As DataGridView) As Control
            Dim card As New RoundedPanel With {.Dock = DockStyle.Fill, .FillColor = Theme.CardBg, .BackColor = Theme.CardBg, .Padding = New Padding(6)}
            card.Controls.Add(g)
            Return card
        End Function
    End Class
End Namespace