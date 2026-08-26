Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports SublimationInventory.Data
Imports SublimationInventory.Models
Imports SublimationInventory.Services
Imports SublimationInventory.UI

Namespace Forms
    Public Class DashboardControl
        Inherits UserControl

        Private grid As DataGridView
        Private chartPanel As Panel
        Private movement As List(Of KeyValuePair(Of DateTime, Integer))
        Private categories As List(Of KeyValuePair(Of String, Integer))
        Private topValue As List(Of ItemStock)
        Private lblChartTitle As Label

        ''' <summary>How many days the movement chart covers - widened automatically when recent days are empty.</summary>
        Private chartDays As Integer = 7

        Public Sub New()
            Me.BackColor = Theme.ContentBg
            Me.Padding = New Padding(28, 22, 28, 22)
            Build()
        End Sub

        Private Sub Build()
            Dim root As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 4, .BackColor = Theme.ContentBg}
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 46))    ' title
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 116))   ' KPI row
            root.RowStyles.Add(New RowStyle(SizeType.Percent, 52.0F))  ' chart + category
            root.RowStyles.Add(New RowStyle(SizeType.Percent, 48.0F))  ' activity + top value
            Me.Controls.Add(root)

            root.Controls.Add(New Label With {.Text = "Dashboard", .Font = Theme.AppFont(20.0F, FontStyle.Bold),
                                              .ForeColor = Theme.TextDark, .AutoSize = True}, 0, 0)

            ' --- KPI cards (all values from InventoryService) ---
            Dim kpis As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 4, .RowCount = 1, .BackColor = Theme.ContentBg}
            For i = 0 To 3
                kpis.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25.0F))
            Next
            root.Controls.Add(kpis, 0, 1)

            Dim lowCount = InventoryService.GetLowStockCount()
            kpis.Controls.Add(KpiCard("ITEMS IN STOCK", InventoryService.GetTotalUnitsInStock().ToString(), "units on hand", False), 0, 0)
            kpis.Controls.Add(KpiCard("LOW STOCK ITEMS", lowCount.ToString(), LowStockCaption(lowCount), lowCount > 0), 1, 0)
            kpis.Controls.Add(KpiCard("INVENTORY VALUE", UiHelpers.Money(InventoryService.GetTotalInventoryValue()), "total stock value", False), 2, 0)
            kpis.Controls.Add(KpiCard("TRANSACTIONS", TransactionRepository.CountThisMonth().ToString(), "this month", False), 3, 0)

            ' --- Middle band: movement chart (left) + stock by category (right) ---
            Dim mid As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1, .BackColor = Theme.ContentBg}
            mid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 58.0F))
            mid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 42.0F))
            mid.Padding = New Padding(0, 14, 0, 0)
            root.Controls.Add(mid, 0, 2)

            mid.Controls.Add(BuildChartCard(), 0, 0)
            mid.Controls.Add(BuildCategoryCard(), 1, 0)

            ' --- Bottom band: recent activity (left) + highest-value stock (right) ---
            Dim bottom As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1, .BackColor = Theme.ContentBg}
            bottom.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 58.0F))
            bottom.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 42.0F))
            bottom.Padding = New Padding(0, 14, 0, 0)
            root.Controls.Add(bottom, 0, 3)

            bottom.Controls.Add(BuildActivityCard(), 0, 0)
            bottom.Controls.Add(BuildTopValueCard(), 1, 0)
        End Sub

        ''' <summary>Explains a zero differently from a real count - "0" alone reads as broken.</summary>
        Private Function LowStockCaption(count As Integer) As String
            If count > 0 Then Return "at / below reorder level"
            If Not InventoryService.AnyReorderLevelSet() Then Return "no reorder levels set yet"
            Return "all items above reorder level"
        End Function

        Private Function KpiCard(title As String, value As String, caption As String, alert As Boolean) As Control
            Dim fill = If(alert, Theme.SidebarBg, Theme.CardBg)
            Dim card As New RoundedPanel With {.Dock = DockStyle.Fill, .Margin = New Padding(0, 0, 14, 0), .FillColor = fill, .BackColor = Theme.ContentBg}
            Dim valueColor = If(alert, Theme.Accent, Theme.TextDark)
            ' Muted grey is unreadable on the navy alert card - use the on-navy tint there.
            Dim labelColor = If(alert, Theme.TextOnNavy, Theme.TextMuted)
            card.Controls.Add(New Label With {.Text = title, .Font = Theme.AppFont(8.5F, FontStyle.Bold), .ForeColor = labelColor,
                                              .AutoSize = True, .Location = New Point(16, 14), .BackColor = fill})
            ' Money values need a smaller face so long figures do not clip.
            Dim valueFont = If(value.Length > 8, Theme.AppFont(19.0F, FontStyle.Bold), Theme.AppFont(26.0F, FontStyle.Bold))
            card.Controls.Add(New Label With {.Text = value, .Font = valueFont, .ForeColor = valueColor,
                                              .AutoSize = True, .Location = New Point(14, If(value.Length > 8, 40, 32)), .BackColor = fill})
            card.Controls.Add(New Label With {.Text = caption, .Font = Theme.AppFont(8.0F), .ForeColor = labelColor,
                                              .AutoSize = True, .Location = New Point(16, 78), .BackColor = fill})
            Return card
        End Function

        ' ---------------- movement chart ----------------
        Private Function BuildChartCard() As Control
            Dim card As New RoundedPanel With {.Dock = DockStyle.Fill, .Margin = New Padding(0, 0, 14, 0), .FillColor = Theme.CardBg, .BackColor = Theme.ContentBg}

            ' Widen the window until it actually contains movement, so the card is never a blank box.
            movement = TransactionRepository.DailyMovement(7)
            chartDays = 7
            If Not movement.Any(Function(kv) kv.Value > 0) Then
                For Each span In New Integer() {30, 90, 365}
                    movement = TransactionRepository.DailyMovement(span)
                    chartDays = span
                    If movement.Any(Function(kv) kv.Value > 0) Then Exit For
                Next
            End If

            lblChartTitle = New Label With {.Text = ChartTitle(), .Font = Theme.AppFont(11.0F, FontStyle.Bold),
                                            .ForeColor = Theme.TextDark, .AutoSize = True, .Location = New Point(16, 14), .BackColor = Theme.CardBg}
            card.Controls.Add(lblChartTitle)

            chartPanel = New Panel With {.Location = New Point(14, 46), .BackColor = Theme.CardBg}
            AddHandler card.Resize, Sub(s, e)
                                        chartPanel.Size = New Size(Math.Max(10, card.Width - 32), Math.Max(10, card.Height - 62))
                                        chartPanel.Invalidate()
                                    End Sub
            chartPanel.Size = New Size(360, 180)
            AddHandler chartPanel.Paint, AddressOf PaintChart
            card.Controls.Add(chartPanel)
            Return card
        End Function

        Private Function ChartTitle() As String
            If movement Is Nothing OrElse Not movement.Any(Function(kv) kv.Value > 0) Then
                Return "Stock movement - no activity recorded yet"
            End If
            Select Case chartDays
                Case 7 : Return "Stock movement - last 7 days"
                Case 30 : Return "Stock movement - last 30 days"
                Case 90 : Return "Stock movement - last 90 days"
                Case Else : Return "Stock movement - last 12 months"
            End Select
        End Function

        Private Sub PaintChart(sender As Object, e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim p = DirectCast(sender, Panel)

            If movement Is Nothing OrElse movement.Count = 0 OrElse Not movement.Any(Function(kv) kv.Value > 0) Then
                DrawEmptyState(g, p, "No stock movement recorded yet.", "Record a stock in or out to see activity here.")
                Return
            End If

            Dim maxV = movement.Max(Function(kv) kv.Value)
            If maxV < 1 Then maxV = 1

            Dim padL = 40, padB = 26, padT = 10, padR = 8
            Dim areaW = p.Width - padL - padR
            Dim areaH = p.Height - padB - padT
            If areaW <= 10 OrElse areaH <= 10 Then Return

            ' horizontal gridlines + y axis labels
            Using gp As New Pen(Theme.ChartGrid)
                Using f = Theme.AppFont(7.5F)
                    For i = 0 To 4
                        Dim yy = padT + CInt(areaH * i / 4.0)
                        g.DrawLine(gp, padL, yy, padL + areaW, yy)
                        Dim val = CInt(maxV * (4 - i) / 4.0)
                        TextRenderer.DrawText(g, val.ToString(), f, New Rectangle(0, yy - 8, padL - 6, 16),
                                              Theme.TextMuted, TextFormatFlags.Right Or TextFormatFlags.VerticalCenter)
                    Next
                End Using
            End Using

            Dim n = movement.Count
            Dim slot = areaW / CSng(n)

            ' Many points read better as a line; few read better as bars.
            If n > 14 Then
                Dim pts(n - 1) As PointF
                For i = 0 To n - 1
                    pts(i) = New PointF(padL + slot * i + slot / 2.0F,
                                        padT + areaH - CSng(movement(i).Value / maxV) * areaH)
                Next
                ' area fill under the line
                Dim poly As New List(Of PointF)(pts)
                poly.Insert(0, New PointF(pts(0).X, padT + areaH))
                poly.Add(New PointF(pts(n - 1).X, padT + areaH))
                Using b As New SolidBrush(Theme.ChartFill)
                    g.FillPolygon(b, poly.ToArray())
                End Using
                Using linePen As New Pen(Theme.ChartLine, 2.0F)
                    linePen.LineJoin = LineJoin.Round
                    g.DrawLines(linePen, pts)
                End Using
                ' mark only the peak so the line stays clean
                Dim peak = movement.Select(Function(kv, i) New With {.i = i, .v = kv.Value}).OrderByDescending(Function(x) x.v).First()
                Using b As New SolidBrush(Theme.Accent)
                    g.FillEllipse(b, pts(peak.i).X - 4, pts(peak.i).Y - 4, 8, 8)
                End Using
            Else
                Dim barW = CInt(Math.Max(6, slot * 0.5F))
                For i = 0 To n - 1
                    Dim kv = movement(i)
                    Dim barH = CInt((kv.Value / CSng(maxV)) * areaH)
                    Dim bx = CInt(padL + slot * i + (slot - barW) / 2.0F)
                    Dim by = padT + areaH - Math.Max(barH, If(kv.Value > 0, 2, 0))
                    If kv.Value > 0 Then
                        Using b As New SolidBrush(Theme.Accent)
                            Using path = Theme.RoundedRect(New Rectangle(bx, by, barW, Math.Max(barH, 2)), 4)
                                g.FillPath(b, path)
                            End Using
                        End Using
                        Using f = Theme.AppFont(7.5F, FontStyle.Bold)
                            TextRenderer.DrawText(g, kv.Value.ToString(), f,
                                                  New Rectangle(CInt(padL + slot * i), by - 15, CInt(slot), 14),
                                                  Theme.TextDark, TextFormatFlags.HorizontalCenter)
                        End Using
                    End If
                Next
            End If

            ' x axis labels - thinned so they never overlap
            Dim step_ = Math.Max(1, CInt(Math.Ceiling(n / 8.0)))
            Using f = Theme.AppFont(7.5F)
                For i = 0 To n - 1 Step step_
                    Dim lbl = If(chartDays <= 7, movement(i).Key.ToString("ddd"), movement(i).Key.ToString("MMM d"))
                    Dim lw = 58
                    Dim cxp = CInt(padL + slot * i + slot / 2.0F)
                    ' Clamp so the first and last labels are not cut off by the panel edge.
                    Dim lx = Math.Max(0, Math.Min(p.Width - lw, cxp - lw \ 2))
                    TextRenderer.DrawText(g, lbl, f,
                                          New Rectangle(lx, p.Height - padB + 4, lw, 16),
                                          Theme.TextMuted, TextFormatFlags.HorizontalCenter Or TextFormatFlags.NoPrefix)
                Next
            End Using
        End Sub

        ' ---------------- stock by category ----------------
        Private Function BuildCategoryCard() As Control
            Dim card As New RoundedPanel With {.Dock = DockStyle.Fill, .FillColor = Theme.CardBg, .BackColor = Theme.ContentBg}
            card.Controls.Add(New Label With {.Text = "Stock by category", .Font = Theme.AppFont(11.0F, FontStyle.Bold),
                                              .ForeColor = Theme.TextDark, .AutoSize = True, .Location = New Point(16, 14), .BackColor = Theme.CardBg})

            categories = InventoryService.GetItemsWithStock().
                GroupBy(Function(s) If(String.IsNullOrWhiteSpace(s.Item.Category), "Uncategorised", s.Item.Category)).
                Select(Function(gr) New KeyValuePair(Of String, Integer)(gr.Key, gr.Sum(Function(s) Math.Max(0, s.OnHand)))).
                OrderByDescending(Function(kv) kv.Value).ToList()

            Dim body As New Panel With {.Location = New Point(16, 46), .BackColor = Theme.CardBg}
            AddHandler card.Resize, Sub(s, e)
                                        body.Size = New Size(Math.Max(10, card.Width - 34), Math.Max(10, card.Height - 62))
                                        body.Invalidate()
                                    End Sub
            body.Size = New Size(300, 180)
            AddHandler body.Paint, AddressOf PaintCategories
            card.Controls.Add(body)
            Return card
        End Function

        Private Sub PaintCategories(sender As Object, e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim p = DirectCast(sender, Panel)

            If categories Is Nothing OrElse categories.Count = 0 Then
                DrawEmptyState(g, p, "No supplies yet.", "Add items on the Supplies screen.")
                Return
            End If

            Dim maxV = Math.Max(1, categories.Max(Function(kv) kv.Value))
            Dim rowH = Math.Min(46, Math.Max(28, p.Height \ Math.Max(1, categories.Count)))
            Dim labelW = 128
            Dim y = 0

            For Each kv In categories
                If y + rowH > p.Height Then Exit For
                Using f = Theme.AppFont(9.0F)
                    TextRenderer.DrawText(g, kv.Key, f, New Rectangle(0, y, labelW - 8, rowH - 8),
                                          Theme.TextDark, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or
                                          TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPrefix)
                End Using

                Dim trackX = labelW
                Dim trackW = p.Width - labelW - 46
                If trackW > 10 Then
                    Dim barH = 12
                    Dim barY = y + (rowH - 8 - barH) \ 2
                    Using b As New SolidBrush(Theme.CardBgAlt)
                        Using path = Theme.RoundedRect(New Rectangle(trackX, barY, trackW, barH), 6)
                            g.FillPath(b, path)
                        End Using
                    End Using
                    Dim w = CInt(trackW * (kv.Value / CSng(maxV)))
                    If w > 0 Then
                        Using b As New SolidBrush(Theme.Accent)
                            Using path = Theme.RoundedRect(New Rectangle(trackX, barY, Math.Max(w, 6), barH), 6)
                                g.FillPath(b, path)
                            End Using
                        End Using
                    End If
                    Using f = Theme.AppFont(9.0F, FontStyle.Bold)
                        TextRenderer.DrawText(g, kv.Value.ToString(), f,
                                              New Rectangle(trackX + trackW + 6, y, 40, rowH - 8),
                                              Theme.TextDark, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter)
                    End Using
                End If
                y += rowH
            Next
        End Sub

        ' ---------------- highest value stock ----------------
        Private Function BuildTopValueCard() As Control
            Dim card As New RoundedPanel With {.Dock = DockStyle.Fill, .FillColor = Theme.CardBg, .BackColor = Theme.ContentBg}
            card.Controls.Add(New Label With {.Text = "Highest value on hand", .Font = Theme.AppFont(11.0F, FontStyle.Bold),
                                              .ForeColor = Theme.TextDark, .AutoSize = True, .Location = New Point(16, 14), .BackColor = Theme.CardBg})

            topValue = InventoryService.GetItemsWithStock().
                       Where(Function(s) s.StockValue > 0).
                       OrderByDescending(Function(s) s.StockValue).Take(6).ToList()

            Dim body As New Panel With {.Location = New Point(16, 46), .BackColor = Theme.CardBg}
            AddHandler card.Resize, Sub(s, e)
                                        body.Size = New Size(Math.Max(10, card.Width - 34), Math.Max(10, card.Height - 62))
                                        body.Invalidate()
                                    End Sub
            body.Size = New Size(300, 180)
            AddHandler body.Paint, AddressOf PaintTopValue
            card.Controls.Add(body)
            Return card
        End Function

        Private Sub PaintTopValue(sender As Object, e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim p = DirectCast(sender, Panel)

            If topValue Is Nothing OrElse topValue.Count = 0 Then
                DrawEmptyState(g, p, "No stock value yet.", "Record stock in to build up value.")
                Return
            End If

            Dim maxV = topValue.Max(Function(s) s.StockValue)
            If maxV <= 0 Then maxV = 1
            Dim rowH = Math.Min(40, Math.Max(26, p.Height \ Math.Max(1, topValue.Count)))
            Dim y = 0

            For Each s In topValue
                If y + rowH > p.Height Then Exit For
                Using f = Theme.AppFont(9.0F)
                    TextRenderer.DrawText(g, s.Item.Name, f, New Rectangle(0, y, p.Width - 96, 16),
                                          Theme.TextDark, TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPrefix)
                End Using
                Using f = Theme.AppFont(9.0F, FontStyle.Bold)
                    TextRenderer.DrawText(g, UiHelpers.Money(s.StockValue), f,
                                          New Rectangle(p.Width - 94, y, 94, 16),
                                          Theme.TextDark, TextFormatFlags.Right)
                End Using
                Dim barY = y + 18
                Dim barW = p.Width
                Using b As New SolidBrush(Theme.CardBgAlt)
                    Using path = Theme.RoundedRect(New Rectangle(0, barY, barW, 6), 3)
                        g.FillPath(b, path)
                    End Using
                End Using
                Dim w = CInt(barW * CSng(s.StockValue / maxV))
                If w > 0 Then
                    Using b As New SolidBrush(Theme.ChartBar)
                        Using path = Theme.RoundedRect(New Rectangle(0, barY, Math.Max(w, 4), 6), 3)
                            g.FillPath(b, path)
                        End Using
                    End Using
                End If
                y += rowH
            Next
        End Sub

        ' ---------------- recent activity ----------------
        Private Function BuildActivityCard() As Control
            Dim card As New RoundedPanel With {.Dock = DockStyle.Fill, .Margin = New Padding(0, 0, 14, 0), .FillColor = Theme.CardBg,
                                               .BackColor = Theme.ContentBg, .Padding = New Padding(14, 44, 14, 14)}
            card.Controls.Add(New Label With {.Text = "Recent activity", .Font = Theme.AppFont(11.0F, FontStyle.Bold),
                                              .ForeColor = Theme.TextDark, .AutoSize = True, .Location = New Point(16, 14), .BackColor = Theme.CardBg})
            grid = New DataGridView With {.Dock = DockStyle.Fill}
            UiHelpers.StyleGrid(grid)
            grid.Columns.Add("Date", "Date")
            grid.Columns.Add("Item", "Item")
            grid.Columns.Add("Type", "Type")
            grid.Columns.Add("Qty", "Qty")
            UiHelpers.SetMinColumnWidths(grid, 120, 150, 70, 60)
            UiHelpers.SetFillWeights(grid, 30, 36, 17, 17)
            grid.Columns("Qty").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            card.Controls.Add(grid)

            For Each t In TransactionRepository.Query(topN:=10)
                Dim idx = grid.Rows.Add(t.TransactionDate.ToString("MMM d, yyyy HH:mm"), t.ItemName, t.Type, t.Quantity)
                grid.Rows(idx).Cells("Type").Style.ForeColor = If(t.Type = "Out", Theme.DangerRed, Theme.OkGreen)
                grid.Rows(idx).Cells("Type").Style.Font = Theme.AppFont(10.5F, FontStyle.Bold)
            Next
            Return card
        End Function

        ''' <summary>Shared "nothing to show" message so empty cards explain themselves.</summary>
        Private Sub DrawEmptyState(g As Graphics, p As Panel, heading As String, hint As String)
            Using f = Theme.AppFont(10.0F, FontStyle.Bold)
                TextRenderer.DrawText(g, heading, f, New Rectangle(0, p.Height \ 2 - 18, p.Width, 20),
                                      Theme.TextMuted, TextFormatFlags.HorizontalCenter)
            End Using
            Using f = Theme.AppFont(8.5F)
                TextRenderer.DrawText(g, hint, f, New Rectangle(0, p.Height \ 2 + 4, p.Width, 18),
                                      Theme.TextMuted, TextFormatFlags.HorizontalCenter)
            End Using
        End Sub
    End Class
End Namespace
