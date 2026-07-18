Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports SublimationInventory.Data
Imports SublimationInventory.Services
Imports SublimationInventory.UI

Namespace Forms
    Public Class DashboardControl
        Inherits UserControl

        Private grid As DataGridView
        Private chartPanel As Panel
        Private movement As List(Of KeyValuePair(Of DateTime, Integer))

        Public Sub New()
            Me.BackColor = Theme.ContentBg
            Me.Padding = New Padding(28, 22, 28, 22)
            Build()
        End Sub

        Private Sub Build()
            Dim root As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 3, .BackColor = Theme.ContentBg}
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 46))
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 116))
            root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
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
            kpis.Controls.Add(KpiCard("LOW STOCK ITEMS", lowCount.ToString(), "at / below reorder level", lowCount > 0), 1, 0)
            kpis.Controls.Add(KpiCard("TODAY'S MOVEMENTS", TransactionRepository.CountToday().ToString(), "stock in/out today", False), 2, 0)
            kpis.Controls.Add(KpiCard("TRANSACTIONS", TransactionRepository.CountThisMonth().ToString(), "this month", False), 3, 0)

            ' --- Bottom: chart (left) + recent activity (right) ---
            Dim bottom As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1, .BackColor = Theme.ContentBg}
            bottom.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 46.0F))
            bottom.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 54.0F))
            bottom.Padding = New Padding(0, 14, 0, 0)
            root.Controls.Add(bottom, 0, 2)

            bottom.Controls.Add(BuildChartCard(), 0, 0)
            bottom.Controls.Add(BuildActivityCard(), 1, 0)
        End Sub

        Private Function KpiCard(title As String, value As String, caption As String, alert As Boolean) As Control
            Dim fill = If(alert, Theme.SecondaryDark, Theme.CardBg)
            Dim card As New RoundedPanel With {.Dock = DockStyle.Fill, .Margin = New Padding(0, 0, 14, 0), .FillColor = fill, .BackColor = Theme.ContentBg}
            Dim valueColor = If(alert, Theme.Accent, Theme.TextDark)
            card.Controls.Add(New Label With {.Text = title, .Font = Theme.AppFont(8.5F, FontStyle.Bold), .ForeColor = Theme.TextMuted,
                                              .AutoSize = True, .Location = New Point(16, 14), .BackColor = fill})
            card.Controls.Add(New Label With {.Text = value, .Font = Theme.AppFont(26.0F, FontStyle.Bold), .ForeColor = valueColor,
                                              .AutoSize = True, .Location = New Point(13, 32), .BackColor = fill})
            card.Controls.Add(New Label With {.Text = caption, .Font = Theme.AppFont(8.0F), .ForeColor = Theme.TextMuted,
                                              .AutoSize = True, .Location = New Point(16, 78), .BackColor = fill})
            Return card
        End Function

        Private Function BuildChartCard() As Control
            Dim card As New RoundedPanel With {.Dock = DockStyle.Fill, .Margin = New Padding(0, 0, 14, 0), .FillColor = Theme.CardBg, .BackColor = Theme.ContentBg}
            card.Controls.Add(New Label With {.Text = "Stock movement - last 7 days", .Font = Theme.AppFont(11.0F, FontStyle.Bold),
                                              .ForeColor = Theme.TextDark, .AutoSize = True, .Location = New Point(16, 14), .BackColor = Theme.CardBg})
            movement = TransactionRepository.DailyMovement(7)
            chartPanel = New Panel With {.Location = New Point(14, 46), .BackColor = Theme.CardBg}
            AddHandler card.Resize, Sub(s, e)
                                        chartPanel.Size = New Size(card.Width - 28, card.Height - 60)
                                        chartPanel.Invalidate()
                                    End Sub
            chartPanel.Size = New Size(360, 180)
            AddHandler chartPanel.Paint, AddressOf PaintChart
            card.Controls.Add(chartPanel)
            Return card
        End Function

        Private Function BuildActivityCard() As Control
            Dim card As New RoundedPanel With {.Dock = DockStyle.Fill, .FillColor = Theme.CardBg, .BackColor = Theme.ContentBg, .Padding = New Padding(14, 44, 14, 14)}
            card.Controls.Add(New Label With {.Text = "Recent activity", .Font = Theme.AppFont(11.0F, FontStyle.Bold),
                                              .ForeColor = Theme.TextDark, .AutoSize = True, .Location = New Point(16, 14), .BackColor = Theme.CardBg})
            grid = New DataGridView With {.Dock = DockStyle.Fill}
            UiHelpers.StyleGrid(grid)
            grid.Columns.Add("Date", "Date")
            grid.Columns.Add("Item", "Item")
            grid.Columns.Add("Type", "Type")
            grid.Columns.Add("Qty", "Qty")
            card.Controls.Add(grid)

            For Each t In TransactionRepository.Query(topN:=10)
                Dim idx = grid.Rows.Add(t.TransactionDate.ToString("MMM d, HH:mm"), t.ItemName, t.Type, t.Quantity)
                grid.Rows(idx).Cells("Type").Style.ForeColor = If(t.Type = "Out", Color.Firebrick, Color.SeaGreen)
            Next
            Return card
        End Function

        Private Sub PaintChart(sender As Object, e As PaintEventArgs)
            If movement Is Nothing OrElse movement.Count = 0 Then Return
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim p = DirectCast(sender, Panel)
            Dim maxV = 1
            For Each kv In movement
                If kv.Value > maxV Then maxV = kv.Value
            Next
            Dim n = movement.Count
            Dim padL = 6, padB = 22, padT = 6
            Dim areaW = p.Width - padL - 6
            Dim areaH = p.Height - padB - padT
            If areaW <= 0 OrElse areaH <= 0 Then Return
            Dim slot = areaW / n
            Dim barW = CInt(slot * 0.55)
            For i = 0 To n - 1
                Dim kv = movement(i)
                Dim barH = CInt((kv.Value / maxV) * areaH)
                Dim bx = CInt(padL + i * slot + (slot - barW) / 2)
                Dim by = padT + areaH - Math.Max(barH, 2)
                Using b As New SolidBrush(Theme.Accent)
                    Using path = Theme.RoundedRect(New Rectangle(bx, by, barW, Math.Max(barH, 2)), 4)
                        g.FillPath(b, path)
                    End Using
                End Using
                Using f = Theme.AppFont(7.5F)
                    TextRenderer.DrawText(g, kv.Key.ToString("ddd"), f,
                                          New Rectangle(CInt(padL + i * slot), p.Height - padB + 2, CInt(slot), 16),
                                          Theme.TextMuted, TextFormatFlags.HorizontalCenter)
                End Using
            Next
        End Sub
    End Class
End Namespace
